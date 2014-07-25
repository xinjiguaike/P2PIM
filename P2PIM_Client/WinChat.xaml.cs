using System.ComponentModel;
using System.Diagnostics;
using System.Net.Mime;
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

    public partial class WinChat : Window, INotifyPropertyChanged
    {
        public readonly User UserChatTo;
        private bool _isInputting;
        private readonly string _userName;
        private readonly string _localEndPoint;

        private bool _isOnline;

        public bool IsOnline
        {
            get { return _isOnline; }
            set
            {
                _isOnline = value;
                OnPropertyChanged("IsOnline");
            }
        }

        public WinChat(User userChatTo, string localEndPoint, string userName)
        {
            InitializeComponent();
            UserChatTo = userChatTo;
            _localEndPoint = localEndPoint;
            _userName = userName;
            this.Title = UserChatTo.UserName;
            IsOnline = true;
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
            await SendAndAppendChatInfo();
        }

        private async void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter) && tbMessageSend.IsFocused && !_isInputting)
                await SendAndAppendChatInfo();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async Task SendAndAppendChatInfo()
        {
            if (!IsOnline)
            {
                MessageBox.Show("The oppsite has been offline, the message could not be sent!", "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (tbMessageSend.Text.Equals(""))
            {
                MessageBox.Show(this, "The message to send could not be empty!", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            await SendChatMessageAsync(tbMessageSend.Text);
            AppendChatInfo(_userName, DateTime.Now.ToLongTimeString(), tbMessageSend.Text, true);
            tbMessageSend.Text = "";
        }

        private async Task SendChatMessageAsync(string message)
        {
            string[] splitString = UserChatTo.LocalIpEndPoint.Split(':');
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(splitString[0]), int.Parse(splitString[1]));
            UdpClient sendUdpClient = new UdpClient(0);
            
            Trace.TraceInformation("P2PIM Trace =>Sending message to [{0},{1}]", UserChatTo.UserName, UserChatTo.LocalIpEndPoint);
            
            Byte[] bytesSend = Encoding.UTF8.GetBytes(string.Format("chat,{0},{1},{2},{3}", _userName, _localEndPoint, DateTime.Now.ToLongTimeString(), message));
            await sendUdpClient.SendAsync(bytesSend, bytesSend.Length, remoteEndPoint);

            sendUdpClient.Close();
        }

        public void AppendChatInfo(string peerName, string time, string content, bool bMySelf)
        {
            AddChatMessage(peerName + "    " + time + Environment.NewLine + content + Environment.NewLine, bMySelf);
            rtbChatBox.ScrollToEnd();
        }

        private void AddChatMessage(string message, bool bByMySelf)
        {

            Paragraph chatParagraph = new Paragraph { TextAlignment = bByMySelf ? TextAlignment.Right : TextAlignment.Left };
            Border chatBorder = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1, 1, 1, 1),
                Background = bByMySelf ? Brushes.LightBlue : Brushes.WhiteSmoke
            };
            TextBlock chatBlock = new TextBlock
            {
                Margin = new Thickness(5,2,5,2),
                FontWeight = FontWeights.Light,
                FontSize = 20,
                TextWrapping = TextWrapping.Wrap,
                Foreground = bByMySelf ? Brushes.Blue : Brushes.Black,
                Text = message
            };
            chatBorder.Child = chatBlock;
            chatParagraph.Inlines.Add(chatBorder);
            fdChatDocument.Blocks.Add(chatParagraph);
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }


        public delegate void PassDataBetweenWindowHandler(object sender, PassDataWinEventArgs e);
        public event PassDataBetweenWindowHandler PassDataBetweenWindow;

        private void Window_Closed(object sender, EventArgs e)
        {
            PassDataWinEventArgs args = new PassDataWinEventArgs(UserChatTo.LocalIpEndPoint);
            PassDataBetweenWindow(this, args);
            Trace.TraceInformation("P2PIM Trace =>Child I'm Here");
        }

    }
}
