using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Security;
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
        }

        public static void AddHentet(string id, string value)
        {
            HentetRK.SetValue(id, value);
        }

        public static bool IsHentet(string id)
        {
            return HentetRK.GetValue(id) != null;
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
            mailserver = GetSetting<string>("mailserver");
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

        private static RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

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

                // The path to the key where Windows looks for startup applications
                //RegistryKey rkApp = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

                //Path to launch shortcut
                string startPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs) + @"\Lars Pehrsson\EBoks\EBoks autodownloader.appref-ms";

                rkApp.SetValue("eboksdownloader3", startPath);
                // Add the value in the registry so that the application runs at startup
                //rkApp.SetValue("eboksdownloader", Assembly.GetExecutingAssembly().Location);
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