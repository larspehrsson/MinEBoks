using System;
using System.Windows;

namespace MinEBoks
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (Properties.Settings.Default.response == "")
            {
                var konfig = new Konfiguration();
                konfig.ShowDialog();
                Properties.Settings.Default.deviceid = Guid.NewGuid().ToString();
                Properties.Settings.Default.response = GetRandomHexNumber(64);
                Properties.Settings.Default.Save();
            }
        }
    }
}
