using System;
using System.ComponentModel;
using System.Diagnostics;
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

        public MainWindow()
        {
            InitializeComponent();

            settings.HentIDList();

            if (string.IsNullOrEmpty(settings.response) || string.IsNullOrEmpty(settings.brugernavn) ||
                !_eboks.GetSessionForAccountRest())
            {
                RunKonfiguration();
            }

            // Initialize menuItemExit
            var menuItemExit = new MenuItem
            {
                Index = 0,
                Text = "E&xit"
            };
            menuItemExit.Click += MenuItemExitClick;

            // Initialize menuItemHent
            var menuItemHent = new MenuItem
            {
                Index = 1,
                Text = "&Hent"
            };
            menuItemHent.Click += MenuItemHentClick;

            // Initialize menuItemÅbn
            var menuItemÅbn = new MenuItem
            {
                Index = 1,
                Text = "&Åbn hentet"
            };
            menuItemÅbn.Click += MenuItemÅbnClick;

            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add(menuItemExit);
            contextMenu.MenuItems.Add(menuItemHent);
            contextMenu.MenuItems.Add(menuItemÅbn);

            // The ContextMenu property sets the menu that will
            // appear when the systray icon is right clicked.
            settings.Notification.ContextMenu = contextMenu;
            settings.Notification.Icon = new Icon("eboksdownloader.ico");
            settings.Notification.Visible = false;
            settings.Notification.DoubleClick +=
                delegate
                {
                    Show();
                    WindowState = WindowState.Normal;
                };


            // Kontroller hver 4. time
            KontrolTimer.Tick += TimerHentDokumenter;
            KontrolTimer.Interval = 1000*60*240;
            KontrolTimer.Start();
            if (settings.startminimeret)
            {
                settings.Notification.Visible = true;
                Hide();
            }

            HentDokumenter();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
                settings.Notification.Visible = true;
            }
            else
                settings.Notification.Visible = false;

            base.OnStateChanged(e);
        }

        private void TimerHentDokumenter(object myObject, EventArgs myEventArgs)
        {
            System.Threading.Thread.Sleep(1000*60*2);  // Vent 2 minutter hvis pc'en lige er vågnet fra standby
        
            settings.Notification.Visible = true;

            HentDokumenter();

            if (WindowState != WindowState.Minimized)
            {
                settings.Notification.Visible = false;
            }
        }


        private void MenuItemExitClick(object Sender, EventArgs e)
        {
            // Close the form, which closes the application.
            Close();
        }

        private async void MenuItemHentClick(object Sender, EventArgs e)
        {
            await HentDokumenter();
        }

        private async void MenuItemÅbnClick(object Sender, EventArgs e)
        {
            Process.Start(settings.savepath);
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
                Task.Factory.StartNew(() => _eboks.DownloadFromEBoks(progress),
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
            settings.Notification.Visible = false;
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