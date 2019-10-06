using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using IWin32Window = System.Windows.Forms.IWin32Window;
using MessageBox = System.Windows.MessageBox;

namespace MinEBoks
{
    /// <summary>
    ///     Interaction logic for Konfiguration.xaml
    /// </summary>
    public partial class Konfiguration
    {
        // The path to the key where Windows looks for startup applications

        public bool Konfigok;

        public Konfiguration()
        {
            InitializeComponent();

            settings.Get();

            PasswordTB.Text = settings.password;
            AktiveringTB.Text = settings.aktiveringskode;
            BrugernavnTB.Text = settings.brugernavn;

            SavePathTB.Text = settings.savepath;
            MailDNSTB.Text = settings.mailserver;
            MailPortTB.Text = settings.mailserverport.ToString();
            MailUserTB.Text = settings.mailserveruser;
            MailPasswdTB.Text = settings.mailserverpassword;
            MailFromTB.Text = settings.mailfrom;
            MailToTB.Text = settings.mailto;
            MailSSLCB.IsChecked = settings.mailserverssl;
            downloadonlyCB.IsChecked = settings.downloadonly;
            StartMinimizedCB.IsChecked = settings.startminimeret;
            auturunCB.IsChecked = settings.autorun;
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (GemSettings())
                Close();

        }

        private bool GemSettings()
        {
            if (!Directory.Exists(SavePathTB.Text))
            {
                MessageBox.Show("Katalog findes ikke: " + SavePathTB.Text, "Fejl", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            settings.brugernavn = BrugernavnTB.Text;
            settings.password = PasswordTB.Text.Trim();
            settings.aktiveringskode = AktiveringTB.Text.Trim();
            settings.savepath = SavePathTB.Text + (SavePathTB.Text.EndsWith("\\") ? "" : "\\");
            settings.mailserver = MailDNSTB.Text;
            settings.mailserverport = string.IsNullOrEmpty(MailPortTB.Text) ? 0 : int.Parse(MailPortTB.Text);
            settings.mailserveruser = MailUserTB.Text;
            settings.mailserverpassword = MailPasswdTB.Text;
            settings.mailfrom = MailFromTB.Text;
            settings.mailto = MailToTB.Text;
            settings.mailserverssl = MailSSLCB.IsChecked.GetValueOrDefault();
            settings.downloadonly = downloadonlyCB.IsChecked.GetValueOrDefault();
            settings.startminimeret = StartMinimizedCB.IsChecked.GetValueOrDefault();
            settings.autorun = auturunCB.IsChecked.GetValueOrDefault();

            if (string.IsNullOrEmpty(settings.deviceid))
                settings.deviceid = Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(settings.response))
                settings.response = settings.GetRandomHexNumber(64);

            if (!SendTestMail())
            {
                return false;
            }

            var eBoks = new Eboks();
            if (!eBoks.GetSessionForAccountRest())
            {
                MessageBox.Show("Opsætning kunne ikke verificeres.", "Fejl", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            settings.Save();
            Konfigok = true;

            return true;
        }


        private bool SendTestMail()
        {
            if (settings.downloadonly)
                return true;

            // Create a message and set up the recipients.
            var message = new MailMessage(
                settings.mailfrom,
                settings.mailto,
                "Test af mail",
                "")
            {
                From = new MailAddress(settings.mailfrom, "Test af mail")
            };

            //Send the message.
            var mailclient = new SmtpClient(settings.mailserver, settings.mailserverport)
            {
                Credentials =
                    new NetworkCredential(settings.mailserveruser, settings.mailserverpassword),
                EnableSsl = settings.mailserverssl
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

            settings.SletHentetList();
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
            if (GemSettings())
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                var eboks = new Eboks();
                var notification = new NotifyIcon();
                settings.opbyghentet = true;
                var progress =
                    new Progress<string>(
                        s => Console.WriteLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "  " + s));
                eboks.DownloadFromEBoks(progress);
                settings.opbyghentet = false;
                Mouse.OverrideCursor = null;
            }
        }
    }
}