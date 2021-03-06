﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace P2PIM_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private UdpClient _receiveUdpClient;
        private TcpClient _tcpClient;
        private StreamReader _readerStream;
        private bool _isOnline;
        private ObservableCollection<User> _onlineUsersList;
        private List<WinChat> _winChatList;
        

        private string _userName;
        public string UserName
        {
            get
            { return _userName; }
            set
            {
                _userName = value;
                OnPropertyChanged("UserName");
            }
        }

        private string _localIp;
        public string LocalIp
        {
            get { return _localIp; }
            set
            {
                _localIp = value;
                OnPropertyChanged("LocalIp");
            }
        }

        private int _localPort;
        public int LocalPort
        {
            get { return _localPort; }
            set
            {
                _localPort = value;
                OnPropertyChanged("LocalPort");
            }
        }

        private string _serverIp;

        public string ServerIp
        {
            get { return _serverIp; }
            set
            {
                _serverIp = value;
                OnPropertyChanged("ServerIp");
            }
        }

        private int _serverPort;
        public int ServerPort
        {
            get { return _serverPort; }
            set
            {
                _serverPort = value;
                OnPropertyChanged("ServerPort");
            }
        }

        private string _msgRecord;
        public string MsgRecord
        {
            get { return _msgRecord; }
            set
            {
                _msgRecord = value;
                OnPropertyChanged("MsgRecord");
            }
        }

        private string _chatContent;
        public string ChatContent
        {
            get { return _chatContent; }
            set
            {
                _chatContent = value;
                OnPropertyChanged("ChatContent");
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            UserName = "Rudy";
            new Action(async () => await SetLocalIpAsync())();
            Random random = new Random();
            LocalPort = random.Next(1024, 65500);
            ServerIp = "173.39.170.96";
            ServerPort = 61864;
            MsgRecord = "";
            _onlineUsersList = new ObservableCollection<User>();
            _winChatList = new List<WinChat>();
            _receiveUdpClient = null;
            _tcpClient = null;
            _readerStream = null;
            _isOnline = false;

            DataContext = this;
            lvOnlineUser.ItemsSource = _onlineUsersList;
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            btnLogin.IsEnabled = false;
            btnLogout.IsEnabled = true;

            _isOnline = true;

            var task1 = ReceiveMessageAsync();
            var task2 = SendLogInOutMessageAsync("login");

            await Task.WhenAll(task1, task2);            
        }

        private async void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmToQuit()) return;
            await SendLogInOutMessageAsync("logout");
            CloseAllConnection();
            ClearOnlineUsersList();
            _isOnline = false;

            btnLogin.IsEnabled = true;
            btnLogout.IsEnabled = false;
        }

        private async void Window_Closed(object sender, EventArgs e)
        {
            if(_isOnline)
                await SendLogInOutMessageAsync("logout");
            CloseAllConnection();
        }


        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!ConfirmToQuit()) 
                e.Cancel = true;
        }

        private bool ConfirmToQuit()
        {
            bool isOpen = _winChatList.Count > 0;
            if (isOpen)
            {
                var mbResult = MessageBox.Show(this, "You're in chatting, really want to quit?", "", MessageBoxButton.OKCancel);
                if (mbResult == MessageBoxResult.OK)
                {
                    for (int i = 0; i < _winChatList.Count; i++)
                    {
                        _winChatList[i].Close();
                    }
                    return true;
                }
                return false;
            }
            return true;
        }


        private void CloseAllConnection()
        {
            if (_receiveUdpClient != null)
                _receiveUdpClient.Close();
            if (_readerStream != null)
                _readerStream.Close();
            if (_tcpClient != null)
                _tcpClient.Close();
        }

        private async Task SetLocalIpAsync()
        {
            string hostName = Dns.GetHostName();
            IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync(hostName);

            foreach (IPAddress ip in ipHostInfo.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    LocalIp = ip.ToString();
                    return;
                }
            }
            throw new Exception("No IPv4 address for local machine");
        }

        private async Task SendLogInOutMessageAsync(string actionType)
        {
            string message = string.Format("{0},{1},{2}:{3}", actionType, UserName, LocalIp, LocalPort);
            UdpClient sendUdpClient = new UdpClient(0);
            Byte[] bytesSend = Encoding.UTF8.GetBytes(message);

            Trace.TraceInformation("P2PIM Trace =>Sending message[{0}]...", message);

            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort);
            await sendUdpClient.SendAsync(bytesSend, bytesSend.Length, remoteEndPoint);
            sendUdpClient.Close();
            Trace.TraceInformation("P2PIM Trace =>Send completed.");
        }

        private async Task ReceiveMessageAsync()
        {
            Trace.TraceInformation("Begin receive message");
            IPEndPoint localIpEndPoint = new IPEndPoint(IPAddress.Parse(LocalIp), LocalPort);
            _receiveUdpClient = new UdpClient(localIpEndPoint);
            while (true)
            {
                try
                {
                    await ParseResponseAsync();
                }
                catch(Exception ex)
                {
                    Trace.TraceInformation("P2PIM Trace =>Exception:[{0}]", ex.Message);
                    if(_receiveUdpClient != null)
                        _receiveUdpClient.Close();
                    break;
                }
            } 
        }

        private async Task ParseResponseAsync()
        {
            Trace.TraceInformation("Parsing response...");

            var resultReceived = await _receiveUdpClient.ReceiveAsync();
            string request = Encoding.UTF8.GetString(resultReceived.Buffer);

            Trace.TraceInformation("Received string [{0}]", request);

            string[] splitString = request.Split(',');
            if (splitString.Length > 1)
            {
                string requestType = splitString[0];
                string userName = "";
                string ipEndPoint = "";
                if(splitString.Length > 2)
                {
                    userName = splitString[1];
                    ipEndPoint = splitString[2];
                }

                switch (requestType)
                {
                    case "Accept":
                        try 
                        {
                            _tcpClient = new TcpClient();
                            _tcpClient.Connect(ServerIp, int.Parse(splitString[1]));
                            NetworkStream networkStream = _tcpClient.GetStream();
                            _readerStream= new StreamReader(networkStream);
                            new Action(async() =>await GetOnlineUsersListAsync())();
                        }
                        catch(Exception ex)
                        {
                            if (_readerStream != null)
                                _readerStream.Close();
                            if (_tcpClient != null)
                                _tcpClient.Close();

                            Trace.TraceInformation("Tcp connect failed[Exception: {0}]", ex.Message);
                        }
                        break;
                    case "login":
                        Trace.TraceInformation("User {0}[{1}] join", userName, ipEndPoint);
                        _onlineUsersList.Add(new User(userName, ipEndPoint));
                        break;
                    case "logout":
                        for (int i = 0; i < _onlineUsersList.Count; i++)
                        {
                            if (!_onlineUsersList[i].LocalIpEndPoint.Equals(ipEndPoint)) continue;
                            _onlineUsersList.RemoveAt(i);
                            break;
                        }
                        Trace.TraceInformation("User {0}[{1}] exit", userName, ipEndPoint);
                        foreach (var winChat in _winChatList)
                        {
                            if (ipEndPoint.Equals(winChat.UserChatTo.LocalIpEndPoint))
                                winChat.IsOnline = false;
                        }
                        break;
                    case "chat":
                        string peerTime = splitString[3];
                        string chatContent = splitString[4];
                        AppendReceivedChatInfo(userName, ipEndPoint, peerTime, chatContent);
                        break;
                    default:
                        Trace.TraceInformation("Request type is invalid");
                        break;
                }
            }
            else
            {
                Trace.TraceError("Response format is not correct.");
            }
        }

        /*private void DisplayUserList()
        {
            Trace.TraceInformation(">>>Begin Display");
            foreach(var user in OnlineUsersList)
            {
                Trace.TraceInformation("{0}[{1}]", user.UserName, user.LocalIpEndPoint);
            }
            Trace.TraceInformation(">>>End Display");
        }*/

        private async Task GetOnlineUsersListAsync()
        {
            Trace.TraceInformation("Getting online user list...");
            while(true)
            {
                try
                {
                    string resultReceived = await _readerStream.ReadLineAsync();
                    Trace.TraceInformation("Received message:[{0}]", resultReceived);
                    if (resultReceived.EndsWith("end"))
                    {
                        string[] splitString = resultReceived.Split(';');
                        for (int i = 0; i < splitString.Length - 1; i++)
                        {
                            string[] userString = splitString[i].Split(',');
                            var newUser = new User(userString[0], userString[1]);
                            _onlineUsersList.Add(newUser);
                        }
                        break;
                    }
                }
                catch(Exception ex)
                {
                    Trace.TraceError("Get online users list exception[{0}]", ex.Message);
                    break;
                }
            }

            _readerStream.Close();
            _tcpClient.Close();
        }

        private void ClearOnlineUsersList()
        {
            _onlineUsersList.Clear();
        }

        private void AppendReceivedChatInfo(string peerName, string peerEndPoint, string peerTime, string message)
        {
            WinChat peerChat = null;
            foreach (var winChat in _winChatList)
            {
                if (!peerEndPoint.Equals(winChat.UserChatTo.LocalIpEndPoint)) continue;
                peerChat = winChat;
                break;
            }

            if (peerChat == null)
            {
                User peerUser = new User(peerName, peerEndPoint);
                string currentEndPoint = string.Format("{0}:{1}", LocalIp, LocalPort);
                peerChat = new WinChat(peerUser, currentEndPoint, UserName);
                peerChat.PassDataBetweenWindow += ChatWindowClosed;
                _winChatList.Add(peerChat);
                peerChat.Show();
            }

            if(!peerChat.IsActive) peerChat.Activate();
            peerChat.AppendChatInfo(peerName, peerTime, message, false);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void lvItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            User selectedUser = lvOnlineUser.SelectedItem as User;
            if (selectedUser == null) return;//no one selected

            string selectedEndPoint = selectedUser.LocalIpEndPoint;
            string currentEndPoint = string.Format("{0}:{1}", LocalIp, LocalPort);
            if (selectedEndPoint.Equals(currentEndPoint)) return;//the user select himself
            
            WinChat newChat = new WinChat(selectedUser, currentEndPoint, UserName);
            newChat.PassDataBetweenWindow += ChatWindowClosed;
            _winChatList.Add(newChat);
            newChat.Show();
            newChat.Activate();
        }

        private void ChatWindowClosed(object sender, PassDataWinEventArgs e)
        {
            for (int i = 0; i < _winChatList.Count; i++)
            {
                Trace.TraceInformation("P2PIM Trace =>I'm Here1");
                if (!_winChatList[i].UserChatTo.LocalIpEndPoint.Equals(e.LocalIpEndPoint)) continue;
                _winChatList.RemoveAt(i);
                Trace.TraceInformation("P2PIM Trace =>I'm Here2");
                break;
            }
            
        }

        private void TbUserName_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            this.Title = tbUserName.Text;
        }
    }
}
