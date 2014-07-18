using P2PService;
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
using System.Windows.Shapes;

namespace P2PIM_Client
{
    /// <summary>
    /// Interaction logic for WinChat.xaml
    /// </summary>
    public partial class WinChat : Window
    {
        private AsyncService serviceAsync;
        private bool isInputting;

        public WinChat()
        {
            InitializeComponent();
            serviceAsync = new AsyncService();
            this.DataContext = serviceAsync;
        }

        private async void OnSendMessage(object sender, RoutedEventArgs e)
        {
            await serviceAsync.SendMessageAsync(tbMessageSend.Text);
            tbMessageSend.Text = "";
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //Settings.Default.Save();
            serviceAsync.StopConnect();
            serviceAsync.StopListen();
        }

        private async void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if((e.Key == Key.Enter) && tbMessageSend.IsFocused && !isInputting)
            {
                await serviceAsync.SendMessageAsync(tbMessageSend.Text);
                tbMessageSend.Text = "";
            }
             
        }

        private void tbMessageSend_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            isInputting = false;
        }

        private void tbMessageSend_TextChanged(object sender, TextChangedEventArgs e)
        {
            isInputting = true;
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
