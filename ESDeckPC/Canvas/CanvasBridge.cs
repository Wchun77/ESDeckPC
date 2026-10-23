using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace ESDeckPC
{
    /// <summary>
    /// What a Canvas page can ask ESDeckPC to do -- exposed to the page as
    /// window.esdeck (see CanvasRenderer):
    ///   esdeck.action({ action: "launch", target: "notepad.exe" })
    ///   esdeck.action({ action: "hotkey", keys: ["ctrl", "shift", "esc"] })
    ///   esdeck.log("text")             -> ESDeckPC log
    /// action takes the same fields as a Deck button (launch, hotkey, media,
    /// text, power, ...), run by ActionExecutor. Both return promises; action
    /// resolves to ActionExecutor's result text.
    ///
    /// Called by WebView2 on the UI thread.
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class CanvasBridge
    {
        private readonly System.Action<string> _log;

        public CanvasBridge(System.Action<string> log)
        {
            _log = log;
        }

        public string Action(string json)
        {
            DeckButton button;
            try
            {
                button = JsonConvert.DeserializeObject<DeckButton>(json);
            }
            catch (Exception ex)
            {
                _log?.Invoke($"Canvas action: bad JSON ({ex.Message})");
                return "bad JSON";
            }
            string result = ActionExecutor.Run(button);
            _log?.Invoke($"Canvas action [{button?.Label ?? button?.Action}]: {result}");
            return result;
        }

        public void Log(string message)
        {
            _log?.Invoke("Canvas page: " + message);
        }
    }
}
