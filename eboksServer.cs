using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MinEBoks
{
    public class Account
    {
        public string UserId { get; set; }

        public string Password { get; set; }

        public string ActivationCode { get; set; }
        public string DeviceId { get; set; }
        public string response { get; set; }
    }

    public partial class MainWindow
    {
        private static readonly Random Random = new Random();

        private static readonly object _lkAppLog = new object();

        // writes a message to the log file
        private void logMessage(IProgress<string> progress, string format, params object[] args)
        {
            lock (_lkAppLog)
            {
                try
                {
                    var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                    //Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " " + string.Format(format, args));
                    var logFile = exePath + "\\eboksreceiver-" + DateTime.UtcNow.ToString("MM") + ".log";
                    using (var fp = new StreamWriter(logFile, true))
                    {
                        fp.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " " + string.Format(format, args));
                        fp.Flush();
                    }

                    progress.Report(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " " + string.Format(format, args));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("logMessage::Exception: " + ex.Message);
                }
            }
        }

        private static string GetRandomHexNumber(int digits)
        {
            var buffer = new byte[digits / 2];
            Random.NextBytes(buffer);
            var result = string.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (digits % 2 == 0)
                return result.ToLower();
            return (result + Random.Next(16).ToString("x")).ToLower();
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var konfig = new Konfiguration();
            konfig.ShowDialog();

            if (string.IsNullOrEmpty(Properties.Settings.Default.deviceid))
                Properties.Settings.Default.deviceid = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(Properties.Settings.Default.response))
                Properties.Settings.Default.response = GetRandomHexNumber(64);

            Properties.Settings.Default.Save();
        }

        private async void HentMenuItem_OnClick(object sender, RoutedEventArgs e)
        {

            var progress = new Progress<string>(s => listView.Items.Add(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " " + s));
            await Task.Factory.StartNew(() => DownloadFromEBoks(progress), TaskCreationOptions.LongRunning);
        }

        private void DownloadFromEBoks(IProgress<string> progress)
        {
            var account = new Account
            {
                UserId = Properties.Settings.Default.brugernavn,
                Password = Properties.Settings.Default.password,
                ActivationCode = Properties.Settings.Default.aktiveringskode,
                DeviceId = Properties.Settings.Default.deviceid,
                response = Properties.Settings.Default.response
            };

            LoadHentetList();

            logMessage(progress, "Kontrollerer for nye meddelelser");
            GetSessionForAccountRest(account);
            DownloadAll(account, progress);
            logMessage(progress, "Kontrol slut");

            if (Properties.Settings.Default.opbyghentet)
            {
                Properties.Settings.Default.opbyghentet = false;
                Properties.Settings.Default.Save();
            }
        }
    }
}