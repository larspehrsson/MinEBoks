using System;
using System.Collections.Generic;
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
    public partial class Konfiguration : Window
    {
        public bool konfigok = false;

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

            Settings.Default.password = PasswordTB.Text;
            Settings.Default.aktiveringskode = AktiveringTB.Text;
            Settings.Default.savepath = SavePathTB.Text+(SavePathTB.Text.EndsWith("\\")?"":"\\");
            Settings.Default.mailserver = MailDNSTB.Text;
            Settings.Default.mailserverport = int.Parse(MailPortTB.Text);
            Settings.Default.mailserveruser = MailUserTB.Text;
            Settings.Default.mailserverpassword = MailPasswdTB.Text;
            Settings.Default.mailfrom = MailFromTB.Text;
            Settings.Default.mailto = MailToTB.Text;
            Settings.Default.brugernavn = BrugernavnTB.Text;
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

            eboks EBoks = new eboks();
            if (!EBoks.GetSessionForAccountRest())
                return;

            Settings.Default.Save();

            konfigok = true;
            Close();
        }

       
        private bool SendTestMail()
        {

            if (Properties.Settings.Default.downloadonly)
                return true;

            // Create a message and set up the recipients.
            var message = new MailMessage(
                Properties.Settings.Default.mailfrom,
                Properties.Settings.Default.mailto,
                "Test af mail",
                "")
            {
                From = new MailAddress(Properties.Settings.Default.mailfrom, "Test af mail")
            };

            //Send the message.
            var mailclient = new SmtpClient(Properties.Settings.Default.mailserver, Properties.Settings.Default.mailserverport)
            {
                Credentials =
                    new NetworkCredential(Properties.Settings.Default.mailserveruser,
                        Properties.Settings.Default.mailserverpassword),
                EnableSsl = Properties.Settings.Default.mailserverssl,
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
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.SelectedPath = SavePathTB.Text;
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            System.Windows.Forms.IWin32Window win = new OldWindow(source.Handle);
            System.Windows.Forms.DialogResult result = dlg.ShowDialog(win);

            if (result==System.Windows.Forms.DialogResult.OK)
            SavePathTB.Text = dlg.SelectedPath;

        }
    }
}