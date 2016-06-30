using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;
using MinEBoks.Properties;
using IWin32Window = System.Windows.Forms.IWin32Window;
using MessageBox = System.Windows.MessageBox;

namespace MinEBoks
{
    /// <summary>
    ///     Interaction logic for Konfiguration.xaml
    /// </summary>
    public partial class Konfiguration
    {

        public static string GetSetting(string setting)
        {
            return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\EboksAutoDownloader", setting, "NULL").ToString();
        }

        // The path to the key where Windows looks for startup applications
        RegistryKey eboksautodownloaderApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        private static readonly Random Random = new Random();
        public bool Konfigok;

        public Konfiguration()
        {
            InitializeComponent();

            //Microsoft.Win32.RegistryKey exampleregistrykey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\EboksAutoDownloader");
            //var x = exampleregistrykey.GetValue("testsetting");
            //exampleregistrykey.SetValue("testsetting", "test");
            //exampleregistrykey.Close();
			
			
			// Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\MyProgram", "Username", "User1");
			// string username = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\MyProgram", "Username", "NULL").ToString();

            Settings.Default.Upgrade();

            PasswordTB.Password = Eboks.Unprotect(Settings.Default.password);
            AktiveringTB.Password = Eboks.Unprotect(Settings.Default.aktiveringskode);
            BrugernavnTB.Password = Eboks.Unprotect(Settings.Default.brugernavn);

            SavePathTB.Text = Settings.Default.savepath;
            MailDNSTB.Text = Settings.Default.mailserver;
            MailPortTB.Text = Settings.Default.mailserverport.ToString();
            MailUserTB.Text = Settings.Default.mailserveruser;
            MailPasswdTB.Password = Eboks.Unprotect(Settings.Default.mailserverpassword);
            MailFromTB.Text = Settings.Default.mailfrom;
            MailToTB.Text = Settings.Default.mailto;
            MailSSLCB.IsChecked = Settings.Default.mailserverssl;
            downloadonlyCB.IsChecked = Settings.Default.downloadonly;
            StartMinimizedCB.IsChecked = Settings.Default.startminimeret;
            auturunCB.IsChecked = Settings.Default.autorun;

            if (string.IsNullOrEmpty(Settings.Default.deviceid) || string.IsNullOrEmpty(Settings.Default.brugernavn))
                downloadonlyCB.IsChecked = true;
        }

        private void downloadonlyCB_toggled(object sender, RoutedEventArgs e)
        {
            MailDNSTB.IsEnabled = !downloadonlyCB.IsChecked.Value;
            MailPortTB.IsEnabled = !downloadonlyCB.IsChecked.Value;
            MailUserTB.IsEnabled = !downloadonlyCB.IsChecked.Value;
            MailPasswdTB.IsEnabled = !downloadonlyCB.IsChecked.Value;
            MailFromTB.IsEnabled = !downloadonlyCB.IsChecked.Value;
            MailToTB.IsEnabled = !downloadonlyCB.IsChecked.Value;
            MailSSLCB.IsEnabled = !downloadonlyCB.IsChecked.Value;

            maillabel.IsEnabled = !downloadonlyCB.IsChecked.Value;

            dnslabel.IsEnabled = !downloadonlyCB.IsChecked.Value;
            portlabel.IsEnabled = !downloadonlyCB.IsChecked.Value;
            ssllabel.IsEnabled = !downloadonlyCB.IsChecked.Value;
            loginlabel.IsEnabled = !downloadonlyCB.IsChecked.Value;
            passwordlabel.IsEnabled = !downloadonlyCB.IsChecked.Value;
            fromlabel.IsEnabled = !downloadonlyCB.IsChecked.Value;
            mailtolabel.IsEnabled = !downloadonlyCB.IsChecked.Value;
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(SavePathTB.Text))
            {
                MessageBox.Show("Katalog findes ikke: " + SavePathTB.Text, "Fejl", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            Settings.Default.brugernavn =
                Eboks.Protect(string.Join("", BrugernavnTB.Password.Where(char.IsDigit)).PadLeft(10, '0'));
            Settings.Default.password = Eboks.Protect(PasswordTB.Password.Trim());
            Settings.Default.aktiveringskode = Eboks.Protect(AktiveringTB.Password.Trim());

            Settings.Default.savepath = SavePathTB.Text + (SavePathTB.Text.EndsWith("\\") ? "" : "\\");
            Settings.Default.mailserver = MailDNSTB.Text;
            Settings.Default.mailserverport = int.Parse(MailPortTB.Text);
            Settings.Default.mailserveruser = MailUserTB.Text;
            Settings.Default.mailserverpassword = Eboks.Protect(MailPasswdTB.Password);
            Settings.Default.mailfrom = MailFromTB.Text;
            Settings.Default.mailto = MailToTB.Text;
            Settings.Default.mailserverssl = MailSSLCB.IsChecked.GetValueOrDefault();
            Settings.Default.downloadonly = downloadonlyCB.IsChecked.GetValueOrDefault();
            Settings.Default.startminimeret = StartMinimizedCB.IsChecked.GetValueOrDefault();
            Settings.Default.autorun = auturunCB.IsChecked.GetValueOrDefault();

            if (string.IsNullOrEmpty(Settings.Default.deviceid))
                Settings.Default.deviceid = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(Settings.Default.response))
                Settings.Default.response = GetRandomHexNumber(64);
            if (Settings.Default.idhentet == null)
                Settings.Default.idhentet = new StringCollection();

            if (!SendTestMail())
            {
                return;
            }

            var eBoks = new Eboks();
            if (!eBoks.GetSessionForAccountRest())
                return;

            if (Settings.Default.autorun)
            {
                // Add the value in the registry so that the application runs at startup
                eboksautodownloaderApp.SetValue("eboksautodownloader", System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            else
            {
                // Remove the value from the registry so that the application doesn't start
                eboksautodownloaderApp.DeleteValue("eboksautodownloader", false);
            }

            Settings.Default.Save();

            Konfigok = true;
            Close();
        }


        private bool SendTestMail()
        {
            if (Settings.Default.downloadonly)
                return true;

            // Create a message and set up the recipients.
            var message = new MailMessage(
                Settings.Default.mailfrom,
                Settings.Default.mailto,
                "Test af mail",
                "")
            {
                From = new MailAddress(Settings.Default.mailfrom, "Test af mail")
            };

            //Send the message.
            var mailclient = new SmtpClient(Settings.Default.mailserver, Settings.Default.mailserverport)
            {
                Credentials =
                    new NetworkCredential(Settings.Default.mailserveruser,
                        Eboks.Unprotect(Settings.Default.mailserverpassword)),
                EnableSsl = Settings.Default.mailserverssl
            };

            try
            {
                mailclient.Send(message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problemer med at sende post " + ex.Message, "Fejl", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void SavePathTB_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dlg = new FolderBrowserDialog
            {
                SelectedPath = SavePathTB.Text
            };
            var source = PresentationSource.FromVisual(this) as HwndSource;
            var win = new OldWindow(source.Handle);
            var result = dlg.ShowDialog(win);

            if (result == System.Windows.Forms.DialogResult.OK)
                SavePathTB.Text = dlg.SelectedPath;
        }

        private void NulstilHentedeButton_OnClick(object sender, RoutedEventArgs e)
        {
            var svar = MessageBox.Show("Er du sikker på at du vil hente alle dokumenter igen?",
                "Nulstil liste over hentede dokumenter", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);

            if (svar != MessageBoxResult.Yes) return;

            Settings.Default.idhentet = new StringCollection();
            Settings.Default.Save();
        }

        private class OldWindow : IWin32Window
        {
            private readonly IntPtr _handle;

            public OldWindow(IntPtr handle)
            {
                _handle = handle;
            }

            #region IWin32Window Members    

            IntPtr IWin32Window.Handle
            {
                get { return _handle; }
            }

            #endregion
        }


        private void MarkerAltSomHentetButton_OnClick(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            var eboks = new Eboks();
            var notification = new NotifyIcon();
            Settings.Default.opbyghentet = true;
            var progress = new Progress<string>(s => Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "  " + s));
            eboks.DownloadFromEBoks(progress, notification);
            Settings.Default.opbyghentet = false;
            Mouse.OverrideCursor = null;
        }
    }
}