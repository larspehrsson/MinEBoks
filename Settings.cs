using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Security;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MinEBoks
{
    internal class Session
    {
        public string Name { get; set; }

        public string InternalUserId { get; set; }

        public string DeviceId { get; set; }

        public string SessionId { get; set; }

        public string Nonce { get; set; }
    }


    public static class settings
    {
        private const string keyphrase = "NwA3ADUAMQ!AxASDkAMbwAzADcA0";
        private static readonly RegistryKey RK = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\EboksAutoDownloader");

        private static readonly RegistryKey HentetRK =
            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\EboksAutoDownloader\HentetIDs");

        private static readonly Random Random = new Random();
        public static readonly NotifyIcon Notification = new NotifyIcon();

        private static Dictionary<string, string>  HentetIDDictionary = new Dictionary<string, string>();

        private static readonly RegistryKey rkApp =
            Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        static settings()
        {
            Get();
        }

        public static string savepath { get; set; }
        public static string mailserver { get; set; }
        public static int mailserverport { get; set; }
        public static string mailserveruser { get; set; }
        public static string mailserverpassword { get; set; }
        public static string response { get; set; }
        public static string aktiveringskode { get; set; }
        public static string deviceid { get; set; }
        public static string brugernavn { get; set; }
        public static string password { get; set; }
        public static string mailfrom { get; set; }
        public static string mailto { get; set; }
        public static bool mailserverssl { get; set; }
        public static bool downloadonly { get; set; }
        public static bool startminimeret { get; set; }
        public static bool autorun { get; set; }
        public static bool opbyghentet { get; set; }

        public static void SletHentetList()
        {
            Registry.CurrentUser.DeleteSubKey(@"SOFTWARE\EboksAutoDownloader\HentetIDs");
            File.Delete(HentetFilename());
        }

        private static void MigrateToCommonApplicationData()
        {
            var hentid = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\EboksAutoDownloader\HentetIDs", true);
            if (hentid == null)
                return;

            var filename = HentetFilename();

            if (!Directory.Exists(Path.GetDirectoryName(filename)))
                Directory.CreateDirectory(Path.GetDirectoryName(filename));

            using (var sw = new StreamWriter(filename, true))
                foreach (var v in hentid.GetValueNames())
                {
                    sw.WriteLine(v + "	" + HentetRK.GetValue(v));
                }

            Registry.CurrentUser.DeleteSubKey(@"SOFTWARE\EboksAutoDownloader\HentetIDs");
        }

        public static void HentIDList()
        {
            if (!File.Exists(HentetFilename()))
                MigrateToCommonApplicationData();

            var filename = HentetFilename();
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
                Directory.CreateDirectory(Path.GetDirectoryName(filename));


            if (File.Exists(HentetFilename()))
            {
                HentetIDDictionary = File.ReadAllLines(HentetFilename()).ToDictionary(c => c.Split('	')[0], c => c.Split('	')[1]);
            }
        }

        private static string HentetFilename()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"EBoksDownloader\hentet.txt");
        }

        public static void AddHentet(string id, string value)
        {
            using (var sw = new StreamWriter(HentetFilename(), true))
                sw.WriteLine(id + "	" + value);

            HentetIDDictionary.Add(id, value);
        }

        public static bool IsHentet(string id)
        {
            return HentetIDDictionary.ContainsKey(id);
        }

        private static T GetSetting<T>(string setting)
        {
            var value = RK.GetValue(setting);
            if (typeof(T).FullName == "System.Boolean" && value == null)
                value = "False";

            if (value == null || value == "")
                return default(T);

            return (T)Convert.ChangeType(value, typeof(T));
        }

        private static void SetSetting<T>(string setting, T value)
        {
            if (value == null)
                RK.SetValue(setting, "");
            else
                RK.SetValue(setting, value);
        }

        public static void Get()
        {
            password = Unprotect(GetSetting<string>("password"));
            aktiveringskode = Unprotect(GetSetting<string>("aktiveringskode"));
            brugernavn = Unprotect(GetSetting<string>("brugernavn"));
            mailserverpassword = Unprotect(GetSetting<string>("mailserverpassword"));

            savepath = GetSetting<string>("savepath");
            mailserver = GetSetting<string>("mailserver") ?? "smtp.gmail.com";
            mailserverport = GetSetting<int>("mailserverport");
            mailserveruser = GetSetting<string>("mailserveruser");
            mailfrom = GetSetting<string>("mailfrom");
            mailto = GetSetting<string>("mailto");
            mailserverssl = GetSetting<bool>("mailserverssl");
            downloadonly = GetSetting<bool>("downloadonly");
            startminimeret = GetSetting<bool>("startminimeret");
            autorun = GetSetting<bool>("autorun");
            deviceid = GetSetting<string>("deviceid");
            response = GetSetting<string>("response");

            if (string.IsNullOrEmpty(GetSetting<string>("deviceid")) ||
                string.IsNullOrEmpty(GetSetting<string>("brugernavn")))
                downloadonly = true;
        }

        public static void Save()
        {
            if (string.IsNullOrEmpty(response))
                response = GetRandomHexNumber(64);

            if (string.IsNullOrEmpty(deviceid))
                deviceid = Guid.NewGuid().ToString();

            SetSetting("brugernavn", Protect(string.Join("", brugernavn.Where(char.IsDigit)).PadLeft(10, '0')));
            SetSetting("password", Protect(password.Trim()));
            SetSetting("aktiveringskode", Protect(aktiveringskode.Trim()));
            SetSetting("mailserverpassword", Protect(mailserverpassword));
            SetSetting("savepath", savepath + (savepath.EndsWith("\\") ? "" : "\\"));
            SetSetting("mailserver", mailserver);
            SetSetting("mailserverport", mailserverport);
            SetSetting("mailserveruser", mailserveruser);
            SetSetting("mailfrom", mailfrom);
            SetSetting("mailto", mailto);
            SetSetting("mailserverssl", mailserverssl);
            SetSetting("downloadonly", downloadonly);
            SetSetting("startminimeret", startminimeret);
            SetSetting("autorun", autorun);
            SetSetting("response", response);
            SetSetting("deviceid", deviceid);

            if (autorun)
            {
                //Path to launch shortcut
                var startPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs) +
                                @"\Lars Pehrsson\EBoks\EBoks autodownloader.appref-ms";

                rkApp.SetValue("eboksdownloader3", startPath);
            }
            else
            {
                // Remove the value from the registry so that the application doesn't start
                rkApp.DeleteValue("eboksdownloader3", false);
            }
        }

        public static string GetRandomHexNumber(int digits)
        {
            var buffer = new byte[digits / 2];
            Random.NextBytes(buffer);
            var result = string.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (digits % 2 == 0)
                return result.ToLower();
            return (result + Random.Next(16).ToString("x")).ToLower();
        }

        private static string Protect(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var myUnprotectedBytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(myUnprotectedBytes);

            var encodedValue = MachineKey.Protect(myUnprotectedBytes, keyphrase);

            return Convert.ToBase64String(encodedValue);
        }

        private static string Unprotect(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            try
            {
                var stream = Convert.FromBase64String(text);
                return Encoding.UTF8.GetString(stream);

                var decodedValue = MachineKey.Unprotect(stream, keyphrase);
                return Encoding.UTF8.GetString(decodedValue);
            }
            catch (Exception)
            {
                return text;
            }
        }
    }
}