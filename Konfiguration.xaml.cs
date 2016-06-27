using System.Windows;
using MinEBoks.Properties;

namespace MinEBoks
{
    /// <summary>
    ///     Interaction logic for Konfiguration.xaml
    /// </summary>
    public partial class Konfiguration : Window
    {
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.password = PasswordTB.Text;
            Settings.Default.aktiveringskode = AktiveringTB.Text;
            Settings.Default.savepath = SavePathTB.Text;
            Settings.Default.mailserver = MailDNSTB.Text;
            Settings.Default.mailserverport = int.Parse(MailPortTB.Text);
            Settings.Default.mailserveruser = MailUserTB.Text;
            Settings.Default.mailserverpassword = MailPasswdTB.Text;
            Settings.Default.mailfrom = MailFromTB.Text;
            Settings.Default.mailto = MailToTB.Text;
            Settings.Default.brugernavn = BrugernavnTB.Text;
            Settings.Default.mailserverssl = MailSSLCB.IsChecked.GetValueOrDefault();
            Settings.Default.downloadonly = downloadonlyCB.IsChecked.GetValueOrDefault();
            Settings.Default.Save();
            Close();
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
    }
}