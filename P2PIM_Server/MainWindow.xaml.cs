using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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

namespace P2PIM_Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private readonly ObservableCollection<User> _onlineUsersList;
        private int _tcpPort;
        private TcpClient _tcpClient;
        private UdpClient _receiveUdpClient;

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

        public MainWindow()
        {
            InitializeComponent();

            _tcpClient = null;
            _receiveUdpClient = null;
            Random random = new Random();
            ServerPort = random.Next(1024, 65500);
            _onlineUsersList = new ObservableCollection<User>();
            new Action(async() =>await SetServerIpAsync())();

            DataContext = this;
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            btnStop.IsEnabled = true;

            var task1 = ReceiveMessage();
            var task2 = Task.Run(() => ListenClientConnect());
            await Task.WhenAll(task1, task2);         
        }


        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            CloseAllConnection();
            btnStart.IsEnabled = true;
            btnStop.IsEnabled = false;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            CloseAllConnection();
        }

        private void Log(string log)
        {
            Dispatcher.Invoke(() =>
            {
                lbLog.AppendText(string.Format("{0}>>> {1}\n", DateTime.Now.ToLongTimeString(), log));
                lbLog.ScrollToEnd();
            });
        }
               
        private void CloseAllConnection()
        {
            if(_receiveUdpClient != null)
                _receiveUdpClient.Close();
            if(_tcpClient != null)
                _tcpClient.Close();
        }

        private async Task SetServerIpAsync()
        {
            string hostName = Dns.GetHostName();
            IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync(hostName);

            foreach (IPAddress ip in ipHostInfo.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ServerIp = ip.ToString();
                    return;
                }
            }
            throw new Exception("No IPv4 address for server");
        }

        private void ListenClientConnect()// Listening thread
        {
            Random random = new Random();
            _tcpPort = random.Next(ServerPort + 1, 65536);
            IPEndPoint listenPoint = new IPEndPoint(IPAddress.Parse(ServerIp), _tcpPort);
            TcpListener listener = new TcpListener(listenPoint);
            Log(string.Format("Start listening on [{0}]...", listenPoint));
            listener.Start();

            while(true)
            {
                try
                {
                    Trace.TraceInformation("P2PIM Trace =>Accepting request...");
                    _tcpClient = listener.AcceptTcpClient();
                    Log(string.Format("Accept tcp connect request from [{0}]", _tcpClient.Client.RemoteEndPoint));
                    new Action(async () => await SendOnlineUsersList())();//Run in parallel
                }
                catch
                {
                    Log(string.Format("Listen thread [{0}:{1}]", ServerIp, ServerPort));
                    _tcpClient.Close();
                    break;
                }

            }
        }

        private async Task ReceiveMessage() //Receive thread
        {
            IPEndPoint serverIpEndPoint = new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort);
            _receiveUdpClient = new UdpClient(serverIpEndPoint);
            while(true)
            {
                try
                {
                    Trace.TraceInformation("P2PIM Trace =>Parse request async.");
                    await ParseRequestAsync();
                }
                catch(Exception ex)
                {
                    Log(ex.Message);
                    _receiveUdpClient.Close();
                    break;
                }
            }
        }

        private async Task ParseRequestAsync()
        {
            //Log("Begin parse request from client");
            var resultReceived = await _receiveUdpClient.ReceiveAsync();
            string request = Encoding.UTF8.GetString(resultReceived.Buffer);
            
            string[] splitString = request.Split(',');
            if(splitString.Length == 3)
            {
                string requestType = splitString[0];
                string userName = splitString[1];
                string ipEndPoint = splitString[2];

                switch(requestType)
                {
                    case "login":
                        Log(string.Format("User {0}[{1}] join", userName, ipEndPoint));
                        User newUser = new User(userName, ipEndPoint);
                        _onlineUsersList.Add(newUser);
                        await SendAcceptAsync(newUser);
                        await SendBroadcastAsync(request, ipEndPoint);
                        break;
                    case "logout":
                        Log(string.Format("User {0}[{1}] request to log out", userName, ipEndPoint));
                        for (int i = 0; i < _onlineUsersList.Count; i++)
                        {
                            if (_onlineUsersList[i].LocalIpEndPoint.Equals(ipEndPoint))
                            {
                                _onlineUsersList.RemoveAt(i);
                                break;
                            }
                        }

                        Log(string.Format("User {0}[{1}] exit", userName, ipEndPoint));

                        await SendBroadcastAsync(request, ipEndPoint);
                        break;
                    default:
                        Log("Request type is invalid");
                        break;
                }
            }
            else
            {
                Log("Request format is not correct.");
            }
        }

        private async Task SendAcceptAsync(User user)
        {
            Log(string.Format("Accept user {0}[{1}]", user.UserName, user.LocalIpEndPoint));

            UdpClient udpClient = new UdpClient(0);
            Byte[] bytesSend = Encoding.UTF8.GetBytes("Accept," + _tcpPort);
            string[] splitString = user.LocalIpEndPoint.Split(':');
            if(splitString.Length == 2)
            {
                string ip = splitString[0];
                int port = int.Parse(splitString[1]);
                Log(string.Format("Sending accept to user {0}:{1}", ip, port));
                IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                await udpClient.SendAsync(bytesSend, bytesSend.Length, remoteIpEndPoint);
                udpClient.Close();
            }
            else
            {
                Log("The user 'LocalIPEndPoint' format not correct.");
            }
            
        }

        private async Task SendBroadcastAsync(string message, string ipEndPoint)
        {
            Log(string.Format("Broadcast [{0}] to the others", message));

            UdpClient udpClient = new UdpClient(0);
            Byte[] bytesSend = Encoding.UTF8.GetBytes(message);
            foreach (User user in _onlineUsersList)
            {
                if (!user.LocalIpEndPoint.Equals(ipEndPoint))
                {
                    string[] splitString = user.LocalIpEndPoint.Split(':');
                    IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Parse(splitString[0]), int.Parse(splitString[1]));
                    await udpClient.SendAsync(bytesSend, bytesSend.Length, remoteIpEndPoint);
                }
            }

            udpClient.Close();
        }

        private async Task SendOnlineUsersList()
        {
            string strUserList = "";
            foreach(User user in _onlineUsersList)
            {
                strUserList += user.UserName + "," + user.LocalIpEndPoint + ";";
            }
            strUserList += "end";

            NetworkStream networkStream = _tcpClient.GetStream();
            StreamWriter streamWriter = new StreamWriter(networkStream);
            streamWriter.AutoFlush = true;

            Log(string.Format("Send online userlist to [{0}]", _tcpClient.Client.RemoteEndPoint));
            Log(string.Format("Online user list: [{0}]", strUserList));
            await streamWriter.WriteLineAsync(strUserList);

            streamWriter.Close();
            _tcpClient.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
