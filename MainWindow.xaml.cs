using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using MinEBoks.Properties;

namespace MinEBoks
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        readonly Eboks _eboks = new Eboks();

        public MainWindow()
        {
            InitializeComponent();

            //Uri iconUri = new Uri(@"eboksdownloader.ico", UriKind.RelativeOrAbsolute);
            //(this.Parent as Window).Icon = BitmapFrame.Create(iconUri);

            if (Settings.Default.response == "" || Settings.Default.brugernavn == "")
            {
                var konfig = new Konfiguration();
                konfig.ShowDialog();
                if (!konfig.Konfigok)
                    Close();

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

            if (!konfig.Konfigok)
                Close();

        }
    }
}
