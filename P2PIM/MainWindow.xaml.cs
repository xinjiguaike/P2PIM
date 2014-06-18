using P2PIM.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace P2PIM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AsyncService serviceAsync;


        public MainWindow()
        {
            InitializeComponent();
            serviceAsync = new AsyncService();
            this.DataContext = serviceAsync;
        }

        private async void OnStartListen(object sender, RoutedEventArgs e)
        {
            btnStartListen.IsEnabled = false;
            btnStopListen.IsEnabled = true;
            lbListening.Visibility = Visibility.Visible;
            await serviceAsync.StartListenAsync();
        }

        private async void OnSendMessage(object sender, RoutedEventArgs e)
        {
            await serviceAsync.SendMessageAsync();
            
        }

        private void OnStopListen(object sender, RoutedEventArgs e)
        {
            serviceAsync.StopListen();
            btnStartListen.IsEnabled = true;
            btnStopListen.IsEnabled = false;
            lbListening.Visibility = Visibility.Hidden;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Settings.Default.Save();
            serviceAsync.StopConnect();
            serviceAsync.StopListen();
        }

        private async void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if((e.Key == Key.Enter) && tbMessageSend.IsFocused)
            {
                await serviceAsync.SendMessageAsync();
                tbMessageSend.Text = "";
            }
             
        }

    }
}
