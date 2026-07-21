using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ESDeckPC
{
    // ------------------------------------------------------------------
    // Discord RPC config (mirrors discord_config.json)
    // ------------------------------------------------------------------

    public class DiscordConfig
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }

    // ------------------------------------------------------------------
    // Discord RPC client
    // ------------------------------------------------------------------

    public class DiscordRpcClient : IDisposable
    {
        private const string ConfigFileName = "discord_config.json";
        private const string RedirectUri = "http://localhost:3000/";
        private const string TokenEndpoint = "https://discord.com/api/oauth2/token";
        private const string PipeName = "discord-ipc-0";

        private static readonly string[] Scopes = { "rpc", "rpc.voice.read", "rpc.voice.write" };

        public static DiscordRpcClient Instance { get; } = new DiscordRpcClient();

        // Called on connect/disconnect to update FormM status bar
        public event Action<bool> ConnectionChanged;

        // Called when a log message should appear in the log panel
        public event Action<string> LogMessage;

        private DiscordConfig _cfg;
        private NamedPipeClientStream _pipe;
        private bool _connected = false;
        private CancellationTokenSource _readCts;

        public bool IsConnected => _connected;

        private DiscordRpcClient() { }

        // ------------------------------------------------------------------
        // Public: connect (called on startup and on Reconnect click)
        // ------------------------------------------------------------------

        public async Task ConnectAsync()
        {
            try
            {
                LoadConfig();

                if (string.IsNullOrWhiteSpace(_cfg.ClientId))
                {
                    Log("Discord: client_id missing in discord_config.json");
                    return;
                }

                await OpenPipeAsync();
                await HandshakeAsync();

                if (!string.IsNullOrEmpty(_cfg.AccessToken))
                {
                    bool ok = await AuthenticateAsync(_cfg.AccessToken);
                    if (!ok && !string.IsNullOrEmpty(_cfg.RefreshToken))
                    {
                        bool refreshed = await RefreshTokenAsync();
                        if (refreshed)
                            ok = await AuthenticateAsync(_cfg.AccessToken);
                    }

                    if (!ok)
                        await RunOAuth2Async();
                }
                else
                {
                    await RunOAuth2Async();
                }
            }
            catch (Exception ex)
            {
                Log($"Discord: connect failed - {ex.Message}");
                SetConnected(false);
            }
        }

        // ------------------------------------------------------------------
        // Public: voice control commands
        // ------------------------------------------------------------------

        public async Task SetMuteAsync(bool mute)
        {
            if (!AssertConnected()) return;
            await SendCommandAsync("SET_VOICE_SETTINGS", new JObject { ["mute"] = mute });
        }

        public async Task SetDeafAsync(bool deaf)
        {
            if (!AssertConnected()) return;
            await SendCommandAsync("SET_VOICE_SETTINGS", new JObject { ["deaf"] = deaf });
        }

        public async Task SelectVoiceChannelAsync(string channelId)
        {
            if (!AssertConnected()) return;
            await SendCommandAsync("SELECT_VOICE_CHANNEL", new JObject
            {
                ["channel_id"] = channelId,
                ["force"] = true
            });
        }

        public async Task LeaveVoiceChannelAsync()
        {
            if (!AssertConnected()) return;
            await SendCommandAsync("SELECT_VOICE_CHANNEL", new JObject
            {
                ["channel_id"] = null
            });
        }

        // ------------------------------------------------------------------
        // Pipe: open
        // ------------------------------------------------------------------

        private async Task OpenPipeAsync()
        {
            ClosePipe();
            _pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await _pipe.ConnectAsync(3000);
        }

        // ------------------------------------------------------------------
        // Handshake (opcode 0)
        // Discord responds with opcode 1, cmd=DISPATCH, evt=READY
        // ------------------------------------------------------------------

        private async Task HandshakeAsync()
        {
            await WriteFrameAsync(0, new JObject
            {
                ["v"] = 1,
                ["client_id"] = _cfg.ClientId.Trim()
            });

            var (opcode, resp) = await ReadFrameAsync();

            // Check for error response first (code 1003 = protocol error etc.)
            if (resp["code"] != null)
                throw new Exception($"Handshake failed: {resp}");

            if (opcode != 1 ||
                resp["cmd"]?.ToString() != "DISPATCH" ||
                resp["evt"]?.ToString() != "READY")
                throw new Exception($"Handshake unexpected response (op={opcode}): {resp}");

            Log("Discord: handshake OK");
        }

        // ------------------------------------------------------------------
        // Authenticate with existing access token
        // ------------------------------------------------------------------

        private async Task<bool> AuthenticateAsync(string token)
        {
            await SendCommandAsync("AUTHENTICATE", new JObject
            {
                ["access_token"] = token
            });

            var (_, resp) = await ReadFrameAsync();

            if (resp["cmd"]?.ToString() == "AUTHENTICATE" &&
                resp["evt"]?.ToString() != "ERROR")
            {
                SetConnected(true);
                Log("Discord: authenticated");
                StartReadLoop();
                return true;
            }

            Log("Discord: token rejected");
            return false;
        }

        // ------------------------------------------------------------------
        // Refresh access token using refresh token
        // ------------------------------------------------------------------

        private async Task<bool> RefreshTokenAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var body = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string,string>("client_id",     _cfg.ClientId),
                        new KeyValuePair<string,string>("client_secret", _cfg.ClientSecret),
                        new KeyValuePair<string,string>("grant_type",    "refresh_token"),
                        new KeyValuePair<string,string>("refresh_token", _cfg.RefreshToken),
                    });

                    var response = await client.PostAsync(TokenEndpoint, body);
                    string json = await response.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);

                    if (obj["access_token"] == null) return false;

                    _cfg.AccessToken = obj["access_token"].ToString();
                    _cfg.RefreshToken = obj["refresh_token"]?.ToString() ?? _cfg.RefreshToken;
                    SaveConfig();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // ------------------------------------------------------------------
        // Full OAuth2 flow: open browser, listen for callback, exchange code
        // ------------------------------------------------------------------

        private async Task RunOAuth2Async()
        {
            string authUrl = "https://discord.com/oauth2/authorize" +
                             $"?client_id={_cfg.ClientId}" +
                             "&response_type=code" +
                             $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                             $"&scope={string.Join("%20", Scopes)}";

            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
            Log("Discord: browser opened for authorization...");

            string code = await ListenForCallbackAsync();
            if (string.IsNullOrEmpty(code))
            {
                Log("Discord: OAuth2 callback not received (timeout)");
                return;
            }

            await ExchangeCodeAsync(code);
            bool ok = await AuthenticateAsync(_cfg.AccessToken);
            if (!ok)
                Log("Discord: authentication failed after OAuth2");
        }

        // ------------------------------------------------------------------
        // HttpListener: wait for Discord to redirect back with ?code=
        // .NET 4.8 compatible (no .WaitAsync)
        // ------------------------------------------------------------------

        private async Task<string> ListenForCallbackAsync()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(RedirectUri);
            listener.Start();

            var contextTask = listener.GetContextAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60));
            var completed = await Task.WhenAny(contextTask, timeoutTask);

            try
            {
                if (completed != contextTask)
                    return null;

                var context = await contextTask;
                string code = context.Request.QueryString["code"];

                string html = "<html><body><h2>Authorization complete. You can close this tab.</h2></body></html>";
                byte[] buf = Encoding.UTF8.GetBytes(html);
                context.Response.ContentLength64 = buf.Length;
                context.Response.OutputStream.Write(buf, 0, buf.Length);
                context.Response.Close();

                return code;
            }
            finally
            {
                listener.Stop();
                listener.Close();
            }
        }

        // ------------------------------------------------------------------
        // Exchange authorization code for access + refresh token
        // ------------------------------------------------------------------

        private async Task ExchangeCodeAsync(string code)
        {
            using (var client = new HttpClient())
            {
                var body = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("client_id",     _cfg.ClientId),
                    new KeyValuePair<string,string>("client_secret", _cfg.ClientSecret),
                    new KeyValuePair<string,string>("grant_type",    "authorization_code"),
                    new KeyValuePair<string,string>("code",          code),
                    new KeyValuePair<string,string>("redirect_uri",  RedirectUri),
                });

                var response = await client.PostAsync(TokenEndpoint, body);
                string json = await response.Content.ReadAsStringAsync();
                var obj = JObject.Parse(json);

                if (obj["access_token"] == null)
                    throw new Exception($"Token exchange failed: {json}");

                _cfg.AccessToken = obj["access_token"].ToString();
                _cfg.RefreshToken = obj["refresh_token"]?.ToString() ?? "";
                SaveConfig();
                Log("Discord: tokens saved");
            }
        }

        // ------------------------------------------------------------------
        // Background read loop: detects pipe disconnection
        // ------------------------------------------------------------------

        private void StartReadLoop()
        {
            _readCts = new CancellationTokenSource();
            var token = _readCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested && _pipe != null && _pipe.IsConnected)
                        await Task.Delay(1000, token);
                }
                catch (OperationCanceledException) { }
                catch { }
                finally
                {
                    SetConnected(false);
                    Log("Discord: disconnected");
                }
            }, token);
        }

        // ------------------------------------------------------------------
        // Frame I/O  (header: opcode LE int32 + length LE int32)
        // ------------------------------------------------------------------

        private async Task WriteFrameAsync(int opcode, JObject payload)
        {
            string json = payload.ToString(Formatting.None);
            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] frame = new byte[8 + data.Length];

            BitConverter.GetBytes(opcode).CopyTo(frame, 0);
            BitConverter.GetBytes(data.Length).CopyTo(frame, 4);
            data.CopyTo(frame, 8);

            await _pipe.WriteAsync(frame, 0, frame.Length);
            await _pipe.FlushAsync();
        }

        private async Task<(int opcode, JObject payload)> ReadFrameAsync()
        {
            byte[] header = new byte[8];
            int read = 0;
            while (read < 8)
                read += await _pipe.ReadAsync(header, read, 8 - read);

            int opcode = BitConverter.ToInt32(header, 0);
            int len = BitConverter.ToInt32(header, 4);

            byte[] body = new byte[len];
            read = 0;
            while (read < len)
                read += await _pipe.ReadAsync(body, read, len - read);

            string json = Encoding.UTF8.GetString(body);
            return (opcode, JObject.Parse(json));
        }

        // ------------------------------------------------------------------
        // Send a Discord RPC command (opcode 1)
        // ------------------------------------------------------------------

        private async Task SendCommandAsync(string cmd, JObject args)
        {
            await WriteFrameAsync(1, new JObject
            {
                ["cmd"] = cmd,
                ["args"] = args,
                ["nonce"] = Guid.NewGuid().ToString()
            });
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private bool AssertConnected()
        {
            if (_connected) return true;
            Log("Discord: not connected");
            return false;
        }

        private void SetConnected(bool value)
        {
            _connected = value;
            ConnectionChanged?.Invoke(value);
        }

        private void Log(string msg) => LogMessage?.Invoke(msg);

        private void ClosePipe()
        {
            _readCts?.Cancel();
            _readCts = null;
            try { _pipe?.Dispose(); } catch { }
            _pipe = null;
        }

        // ------------------------------------------------------------------
        // Config load / save
        // ------------------------------------------------------------------

        private string ConfigPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

        private void LoadConfig()
        {
            if (!File.Exists(ConfigPath))
                throw new FileNotFoundException($"{ConfigFileName} not found next to EXE");

            _cfg = JsonConvert.DeserializeObject<DiscordConfig>(File.ReadAllText(ConfigPath));
        }

        private void SaveConfig()
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(_cfg, Formatting.Indented));
        }

        public void Dispose() => ClosePipe();
    }
}