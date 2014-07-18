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
        private UdpClient receiveUdpClient;
        private TcpClient tcpClient;
        private StreamReader readerStream;

        private ObservableCollection<User> OnlineUsersList;
        private User ObjChatTo;
        private bool IsOnline;

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

        private string _serverIP;
        public string ServerIP
        {
            get { return _serverIP; }
            set
            {
                _serverIP = value;
                OnPropertyChanged("ServerIP");
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

            IsOnline = false;
            UserName = "Rudy";
            new Action(async () => await SetLocalIPAsync())();
            Random random = new Random();
            LocalPort = random.Next(1024, 65500);
            ServerIP = "10.224.202.82";
            ServerPort = 5000;
            MsgRecord = "";
            OnlineUsersList = new ObservableCollection<User>();
            ObjChatTo = new User("Tmac", "");
            receiveUdpClient = null;
            tcpClient = null;
            readerStream = null;

            DataContext = this;
            lvOnlineUser.ItemsSource = OnlineUsersList;
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            btnLogin.IsEnabled = false;
            btnLogout.IsEnabled = true;

            IsOnline = true;

            var task1 = ReceiveMessageAsync();
            var task2 = SendLogInOutMessageAsync("login");

            await Task.WhenAll(task1, task2);            
        }

        private async void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            await SendLogInOutMessageAsync("logout");
            CloseAllConnection();
            ClearOnlineUsersList();
            IsOnline = false;

            btnLogin.IsEnabled = true;
            btnLogout.IsEnabled = false;
        }

        private async void Window_Closed(object sender, EventArgs e)
        {
            if(IsOnline)
                await SendLogInOutMessageAsync("logout");
            CloseAllConnection();
        }

        

        public void CloseAllConnection()
        {
            if (receiveUdpClient != null)
                receiveUdpClient.Close();
            if (readerStream != null)
                readerStream.Close();
            if (tcpClient != null)
                tcpClient.Close();
        }

        public async Task SetLocalIPAsync()
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
        public async Task SendLogInOutMessageAsync(string actionType)
        {
            string message = string.Format("{0},{1},{2}:{3}", actionType, UserName, LocalIP, LocalPort);
            UdpClient sendUdpClient = new UdpClient(0);
            Byte[] bytesSend = Encoding.UTF8.GetBytes(message);

            Trace.TraceInformation("P2PIM Trace =>Sending message[{0}]...", message);

            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(ServerIP), ServerPort);
            await sendUdpClient.SendAsync(bytesSend, bytesSend.Length, remoteEndPoint);
            sendUdpClient.Close();
            Trace.TraceInformation("P2PIM Trace =>Send completed.");
        }

        public async Task SendChatMessageAsync()
        {
            string[] splitString = ObjChatTo.LocalIPEndPoint.Split(':');
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(splitString[0]), int.Parse(splitString[1]));
            UdpClient sendUdpClient = new UdpClient(0);
            Byte[] bytesSend = Encoding.UTF8.GetBytes(string.Format("chat,{0},{1}", DateTime.Now, ChatContent));
            await sendUdpClient.SendAsync(bytesSend, bytesSend.Length, remoteEndPoint);
            sendUdpClient.Close();
        }

        public async Task ReceiveMessageAsync()
        {
            Trace.TraceInformation("Begin receive message");
            IPEndPoint localIPEndPoint = new IPEndPoint(IPAddress.Parse(LocalIP), LocalPort);
            receiveUdpClient = new UdpClient(localIPEndPoint);
            while (true)
            {
                try
                {
                    await ParseResponseAsync(receiveUdpClient);
                }
                catch(Exception ex)
                {
                    Trace.TraceInformation("P2PIM Trace =>Exception:[{0}]", ex.Message);
                    if(receiveUdpClient != null)
                        receiveUdpClient.Close();
                    break;
                }
            } 
        }

        public async Task ParseResponseAsync(UdpClient udpClient)
        {
            Trace.TraceInformation("Parsing response...");

            var resultReceived = await receiveUdpClient.ReceiveAsync();
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
                            tcpClient = new TcpClient();
                            tcpClient.Connect(ServerIP, int.Parse(splitString[1]));
                            NetworkStream networkStream = tcpClient.GetStream();
                            readerStream= new StreamReader(networkStream);
                            new Action(async() =>await GetOnlineUsersListAsync())();
                        }
                        catch(Exception ex)
                        {
                            if (readerStream != null)
                                readerStream.Close();
                            if (tcpClient != null)
                                tcpClient.Close();

                            Trace.TraceInformation("Tcp connect failed[Exception: {0}]", ex.Message);
                        }
                        break;
                    case "login":
                        Trace.TraceInformation(string.Format("User {0}[{1}] join", userName, ipEndPoint));
                        OnlineUsersList.Add(new User(userName, ipEndPoint));
                        break;
                    case "logout":
                        DisplayUserList();
                        for (int i = 0; i < OnlineUsersList.Count; i++)
                        {
                            if (OnlineUsersList[i].LocalIPEndPoint.Equals(ipEndPoint))
                            {
                                Trace.TraceInformation("Remove index=" + i);
                                OnlineUsersList.RemoveAt(i);
                                break;
                            }
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

        public void DisplayUserList()
        {
            Trace.TraceInformation(">>>Begin Display");
            foreach(var user in OnlineUsersList)
            {
                Trace.TraceInformation("{0}[{1}]", user.UserName, user.LocalIPEndPoint);
            }
            Trace.TraceInformation(">>>End Display");
        }

        public async Task GetOnlineUsersListAsync()
        {
            Trace.TraceInformation("Getting online user list...");
            while(true)
            {
                try
                {
                    string resultReceived = await readerStream.ReadLineAsync();
                    Trace.TraceInformation("Received message:[{0}] exit", resultReceived);
                    if (resultReceived.EndsWith("end"))
                    {
                        string[] splitString = resultReceived.Split(';');
                        for (int i = 0; i < splitString.Length - 1; i++)
                        {
                            string[] userString = splitString[i].Split(',');
                            User newUser = new User(userString[0], userString[1]);
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

            readerStream.Close();
            tcpClient.Close();
        }

        public void ClearOnlineUsersList()
        {
            OnlineUsersList.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
