using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace MinEBoks
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly Timer KontrolTimer = new Timer();
        private readonly Eboks _eboks = new Eboks();
        private readonly NotifyIcon _notification = new NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();

            if (string.IsNullOrEmpty(settings.response) || string.IsNullOrEmpty(settings.brugernavn)  || !_eboks.GetSessionForAccountRest())
            {
                RunKonfiguration();
            }

            // Minimize to systemtray
            _notification.Icon = new Icon("eboksdownloader.ico");
            _notification.Visible = false;
            _notification.DoubleClick +=
                delegate
                {
                    Show();
                    WindowState = WindowState.Normal;
                };

            // Kontroller hver 4. time
            KontrolTimer.Tick += TimerHentDokumenter;
            KontrolTimer.Interval = 1000 * 60 * 240;
            KontrolTimer.Start();
            if (settings.startminimeret)
            {
                _notification.Visible = true;
                Hide();
            }

            HentDokumenter();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                _notification.Visible = true;
            }
            else
                _notification.Visible = false;

            base.OnStateChanged(e);
        }

        private void TimerHentDokumenter(object myObject, EventArgs myEventArgs)
        {
            _notification.Visible = true;

            HentDokumenter();

            if (WindowState != WindowState.Minimized)
            {
                _notification.Visible = false;
            }
        }


        private async void HentMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            await HentDokumenter();
        }

        private async Task HentDokumenter()
        {
            var progress =
                new Progress<string>(
                    s => listView.Items.Insert(0, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "  " + s));
            await
                Task.Factory.StartNew(() => _eboks.DownloadFromEBoks(progress, _notification),
                    TaskCreationOptions.LongRunning);
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            KontrolTimer.Stop();
            RunKonfiguration();
            KontrolTimer.Start();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _notification.Visible = false;
        }

        private void RunKonfiguration()
        {
            var konfig = new Konfiguration();
            konfig.ShowDialog();
            if (!konfig.Konfigok)
                Close();
        }
    }
}