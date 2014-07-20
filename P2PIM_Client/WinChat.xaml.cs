using System.Diagnostics;
using P2PService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        public readonly User UserChatTo;
        private bool _isInputting;
        private readonly string _userName;
        private readonly string _localEndPoint;

        public WinChat(User userChatTo, string localEndPoint, string userName)
        {
            InitializeComponent();
            UserChatTo = userChatTo;
            _localEndPoint = localEndPoint;
            _userName = userName;
            this.Title = UserChatTo.UserName;
        }

        private async void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if((e.Key == Key.Enter) && tbMessageSend.IsFocused && !_isInputting)
            {
                await SendChatMessageAsync(tbMessageSend.Text);
                AppendChatInfo(_userName, DateTime.Now.ToLongTimeString(), tbMessageSend.Text);
                tbMessageSend.Text = "";
            }
             
        }

        private void tbMessageSend_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            _isInputting = false;
        }

        private void tbMessageSend_TextChanged(object sender, TextChangedEventArgs e)
        {
            _isInputting = true;
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            await SendChatMessageAsync(tbMessageSend.Text);
            AppendChatInfo(_userName, DateTime.Now.ToLongTimeString(), tbMessageSend.Text);
            tbMessageSend.Text = "";
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private async Task SendChatMessageAsync(string message)
        {
            if (message.Equals(""))
            {
                MessageBox.Show(this, "The message to send could not be empty!");
                return;
            }
            string[] splitString = UserChatTo.LocalIpEndPoint.Split(':');
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(splitString[0]), int.Parse(splitString[1]));
            UdpClient sendUdpClient = new UdpClient(0);
            
            Trace.TraceInformation("P2PIM Trace =>Sending message to [{0},{1}]", UserChatTo.UserName, UserChatTo.LocalIpEndPoint);
            
            Byte[] bytesSend = Encoding.UTF8.GetBytes(string.Format("chat,{0},{1},{2},{3}", _userName, _localEndPoint, DateTime.Now.ToLongTimeString(), message));
            await sendUdpClient.SendAsync(bytesSend, bytesSend.Length, remoteEndPoint);

            sendUdpClient.Close();
        }

        public void AppendChatInfo(string peerName, string time, string content)
        {
            tbChatContent.AppendText(peerName + "    " + time + Environment.NewLine + content + Environment.NewLine);
            tbChatContent.ScrollToEnd();
        }

    }
}
