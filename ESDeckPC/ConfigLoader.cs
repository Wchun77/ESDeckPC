using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ESDeckPC
{
    public static class ConfigLoader
    {
        // ------------------------------------------------------------------
        // Load
        // ------------------------------------------------------------------

        public static PcConfig LoadPc(string path)
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<PcConfig>(json);
        }

        public static EspConfig LoadEsp(string path)
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<EspConfig>(json);
        }

        // ------------------------------------------------------------------
        // Save
        // ------------------------------------------------------------------

        public static void SavePc(PcConfig config, string path)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public static void SaveEsp(EspConfig config, string path)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        // ------------------------------------------------------------------
        // Save pair - generates pc_XXXX.json + esp_XXXX.json in the same folder
        // Returns the CRC string used in the filenames
        // ------------------------------------------------------------------

        public static string SavePair(PcConfig pcConfig, string folder)
        {
            EspConfig espConfig = BuildEspFromPc(pcConfig);

            string pcJson = JsonConvert.SerializeObject(pcConfig, Formatting.Indented);
            string espJson = JsonConvert.SerializeObject(espConfig, Formatting.Indented);

            ushort crc = Crc16(pcJson);
            string crcStr = crc.ToString("X4");

            File.WriteAllText(Path.Combine(folder, $"pc_{crcStr}.json"), pcJson);
            File.WriteAllText(Path.Combine(folder, $"esp_{crcStr}.json"), espJson);

            return crcStr;
        }

        // ------------------------------------------------------------------
        // Build EspConfig from PcConfig using Label + Icon on each PcButton
        // ------------------------------------------------------------------

        public static EspConfig BuildEspFromPc(PcConfig pcConfig)
        {
            var esp = new EspConfig();

            esp.Settings = new EspSettings
            {
                BgImage = pcConfig.Settings?.BgImage ?? "",
                SideIcon = pcConfig.Settings?.SideIcon ?? "",
            };

            foreach (var pcPage in pcConfig.Pages)
            {
                var espPage = new EspPage
                {
                    Name = pcPage.Name,
                    BgImage = pcPage.BgImage ?? "",
                    SideIcon = pcPage.SideIcon ?? "",
                };

                foreach (var pcBtn in pcPage.Buttons)
                {
                    espPage.Buttons.Add(new EspButton
                    {
                        Label = pcBtn.Label ?? "",
                        Icon = pcBtn.Icon ?? "",
                    });
                }

                esp.Pages.Add(espPage);
            }

            return esp;
        }

        // ------------------------------------------------------------------
        // CRC16 (CRC-CCITT, poly 0x1021, init 0xFFFF)
        // ------------------------------------------------------------------

        public static ushort Crc16(string text)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
            ushort crc = 0xFFFF;

            foreach (byte b in bytes)
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc <<= 1;
                }
            }

            return crc;
        }
    }
}