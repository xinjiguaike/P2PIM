using System;
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

        private ObservableCollection<User> OnlineUsersList;
        private User ObjChatTo;
        private bool _isOnline;

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

        private string _localIP;
        public string LocalIP
        {
            get { return _localIP; }
            set
            {
                _localIP = value;
                OnPropertyChanged("LocalIP");
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

            _isOnline = false;
            UserName = "Rudy";
            new Action(async () => await SetLocalIpAsync())();
            Random random = new Random();
            LocalPort = random.Next(1024, 65500);
            ServerIp = "10.224.202.82";
            ServerPort = 5000;
            MsgRecord = "";
            OnlineUsersList = new ObservableCollection<User>();
            ObjChatTo = null;
            _receiveUdpClient = null;
            _tcpClient = null;
            _readerStream = null;

            DataContext = this;
            lvOnlineUser.ItemsSource = OnlineUsersList;
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
                    LocalIP = ip.ToString();
                    return;
                }
            }
            throw new Exception("No IPv4 address for local machine");
        }

        private async Task SendLogInOutMessageAsync(string actionType)
        {
            string message = string.Format("{0},{1},{2}:{3}", actionType, UserName, LocalIP, LocalPort);
            UdpClient sendUdpClient = new UdpClient(0);
            Byte[] bytesSend = Encoding.UTF8.GetBytes(message);

            Trace.TraceInformation("P2PIM Trace =>Sending message[{0}]...", message);

            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort);
            await sendUdpClient.SendAsync(bytesSend, bytesSend.Length, remoteEndPoint);
            sendUdpClient.Close();
            Trace.TraceInformation("P2PIM Trace =>Send completed.");
        }

        private async Task SendChatMessageAsync()
        {
            string[] splitString = ObjChatTo.LocalIpEndPoint.Split(':');
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(splitString[0]), int.Parse(splitString[1]));
            UdpClient sendUdpClient = new UdpClient(0);
            Byte[] bytesSend = Encoding.UTF8.GetBytes(string.Format("chat,{0},{1}", DateTime.Now, ChatContent));
            await sendUdpClient.SendAsync(bytesSend, bytesSend.Length, remoteEndPoint);
            sendUdpClient.Close();
        }

        private async Task ReceiveMessageAsync()
        {
            Trace.TraceInformation("Begin receive message");
            IPEndPoint localIpEndPoint = new IPEndPoint(IPAddress.Parse(LocalIP), LocalPort);
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
                        OnlineUsersList.Add(new User(userName, ipEndPoint));
                        break;
                    case "logout":
                        DisplayUserList();
                        for (int i = 0; i < OnlineUsersList.Count; i++)
                        {
                            if (!OnlineUsersList[i].LocalIpEndPoint.Equals(ipEndPoint)) continue;
                            Trace.TraceInformation("Remove index=" + i);
                            OnlineUsersList.RemoveAt(i);
                            break;
                        }
                        
                        //OnlineUsersList.Remove(new UserInfo(userName, ipEndPoint));
                        DisplayUserList();
                        Trace.TraceInformation("User {0}[{1}] exit", userName, ipEndPoint);
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

        private void DisplayUserList()
        {
            Trace.TraceInformation(">>>Begin Display");
            foreach(var user in OnlineUsersList)
            {
                Trace.TraceInformation("{0}[{1}]", user.UserName, user.LocalIpEndPoint);
            }
            Trace.TraceInformation(">>>End Display");
        }

        private async Task GetOnlineUsersListAsync()
        {
            Trace.TraceInformation("Getting online user list...");
            while(true)
            {
                try
                {
                    string resultReceived = await _readerStream.ReadLineAsync();
                    Trace.TraceInformation("Received message:[{0}] exit", resultReceived);
                    if (resultReceived.EndsWith("end"))
                    {
                        string[] splitString = resultReceived.Split(';');
                        for (int i = 0; i < splitString.Length - 1; i++)
                        {
                            string[] userString = splitString[i].Split(',');
                            var newUser = new User(userString[0], userString[1]);
                            OnlineUsersList.Add(newUser);
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
            OnlineUsersList.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
