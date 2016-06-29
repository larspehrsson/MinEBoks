using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using MinEBoks.Properties;
using MessageBox = System.Windows.MessageBox;

namespace MinEBoks
{
    /// <summary>
    ///     Interaction logic for Konfiguration.xaml
    /// </summary>
    public partial class Konfiguration
    {
        public bool Konfigok;

        public Konfiguration()
        {
            InitializeComponent();

            PasswordTB.Text = Settings.Default.password;
            AktiveringTB.Text = Settings.Default.aktiveringskode;
            SavePathTB.Text = Settings.Default.savepath;
            MailDNSTB.Text = Settings.Default.mailserver;
            MailPortTB.Text = Settings.Default.mailserverport.ToString();
            MailUserTB.Text = Settings.Default.mailserveruser;
            MailPasswdTB.Text = Settings.Default.mailserverpassword;
            MailFromTB.Text = Settings.Default.mailfrom;
            MailToTB.Text = Settings.Default.mailto;
            BrugernavnTB.Text = Settings.Default.brugernavn;
            MailSSLCB.IsChecked = Settings.Default.mailserverssl;
            downloadonlyCB.IsChecked = Settings.Default.downloadonly;

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


        private static readonly Random Random = new Random();
        
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

            Settings.Default.password = PasswordTB.Text.Trim();
            Settings.Default.aktiveringskode = AktiveringTB.Text.Trim();
            Settings.Default.savepath = SavePathTB.Text+(SavePathTB.Text.EndsWith("\\")?"":"\\");
            Settings.Default.mailserver = MailDNSTB.Text;
            Settings.Default.mailserverport = int.Parse(MailPortTB.Text);
            Settings.Default.mailserveruser = MailUserTB.Text;
            Settings.Default.mailserverpassword = MailPasswdTB.Text;
            Settings.Default.mailfrom = MailFromTB.Text;
            Settings.Default.mailto = MailToTB.Text;
            Settings.Default.brugernavn = String.Join("", BrugernavnTB.Text.Where(char.IsDigit)).PadLeft(10,'0');
            Settings.Default.mailserverssl = MailSSLCB.IsChecked.GetValueOrDefault();
            Settings.Default.downloadonly = downloadonlyCB.IsChecked.GetValueOrDefault();


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

            Eboks eBoks = new Eboks();
            if (!eBoks.GetSessionForAccountRest())
                return;

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
                        Settings.Default.mailserverpassword),
                EnableSsl = Settings.Default.mailserverssl,
            };

            try
            {
                mailclient.Send(message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problemer med at sende post "+ex.Message, "Fejl", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
        private class OldWindow : System.Windows.Forms.IWin32Window
        {
            IntPtr _handle;
            public OldWindow(IntPtr handle)
            {
                _handle = handle;
            }

            #region IWin32Window Members    
            IntPtr System.Windows.Forms.IWin32Window.Handle
            {
                get { return _handle; }
            }
            #endregion
        }

        private void SavePathTB_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = SavePathTB.Text
            };
            var source = PresentationSource.FromVisual(this) as HwndSource;
            var win = new OldWindow(source.Handle);
            var result = dlg.ShowDialog(win);

            if (result==System.Windows.Forms.DialogResult.OK)
            SavePathTB.Text = dlg.SelectedPath;

        }
    }
}