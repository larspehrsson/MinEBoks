using System;
using System.Threading.Tasks;
using System.Windows;

namespace MinEBoks
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly eboks _eboks = new eboks();

        public MainWindow()
        {
            InitializeComponent();

            if (Properties.Settings.Default.response == "")
            {
                var konfig = new Konfiguration();
                konfig.ShowDialog();
                if (!konfig.konfigok)
                    this.Close();

            }
        }

        private async void HentMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var progress =
                new Progress<string>(
                    s => listView.Items.Add(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "  " + s));
            await Task.Factory.StartNew(() => _eboks.DownloadFromEBoks(progress), TaskCreationOptions.LongRunning);
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var konfig = new Konfiguration();
            konfig.ShowDialog();

            if (!konfig.konfigok)
                this.Close();

        }
    }
}
