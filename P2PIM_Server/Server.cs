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

namespace P2PIM_Server
{
    public class UserInfo
    {
        public string UserName;
        public string LocalIPEndPoint;
    
        public UserInfo(string name, string endpoint)
        {
            UserName = name;
            LocalIPEndPoint = endpoint;
        }
    }

    public class Server: INotifyPropertyChanged
    {
        public ObservableCollection<string> LogList;
        public ObservableCollection<UserInfo> OnlineUsersList;
        private int tcpPort;
        private TcpClient tcpClient;

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

        private string _logString;
        public string LogString
        {
            get { return _logString; }
            set
            {
                _logString = value;
                OnPropertyChanged("LogString");
            }
        }
        

        public Server()
        {
            tcpClient = null;
            Random random = new Random();
            ServerPort = random.Next(1024, 65500);
            OnlineUsersList = new ObservableCollection<UserInfo>();
            new Action(async() =>await SetServerIPAsync())();
        }

        public async Task SetServerIPAsync()
        {
            string hostName = Dns.GetHostName();
            IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync(hostName);

            foreach (IPAddress ip in ipHostInfo.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ServerIP = ip.ToString();
                    return;
                }
            }
            throw new Exception("No IPv4 address for server");
        }

        public void Log(string log)
        {
           // LogList.Add(DateTime.Now.ToString() + ">>> " + log);
            LogString += string.Format("{0}>>> {1}\n", DateTime.Now.ToString(), log);
        }

        public void ListenClientConnect()// Listening thread
        {
            Random random = new Random();
            tcpPort = random.Next(ServerPort + 1, 65536);
            IPEndPoint listenPoint = new IPEndPoint(IPAddress.Parse(ServerIP), tcpPort);
            TcpListener listener = new TcpListener(listenPoint);
            Log(string.Format("Start listening on [{0}]...", listenPoint));
            listener.Start();
            //TcpClient tcpClient = null;

            while(true)
            {
                try
                {
                    Trace.TraceInformation("P2PIM Trace =>Accepting request...");
                    tcpClient = listener.AcceptTcpClient();
                    Log(string.Format("Accept tcp connect request from [{0}]", tcpClient.Client.RemoteEndPoint));
                    new Action(async () => await SendOnlineUsersList())();//Run in parallel
                }
                catch
                {
                    Log(string.Format("Listen thread [{0}:{1}]", ServerIP, ServerPort));
                    tcpClient.Close();
                    break;
                }

            }
        }

        public async Task ReceiveMessage() // Receive thread
        {
            IPEndPoint serverIPEndPoint = new IPEndPoint(IPAddress.Parse(ServerIP), ServerPort);
            UdpClient receiveUdpClient = new UdpClient(serverIPEndPoint);
            while(true)
            {
                try
                {
                    Trace.TraceInformation("P2PIM Trace =>Parse request async.");
                    await ParseRequestAsync(receiveUdpClient);
                }
                catch(Exception ex)
                {
                    Log(ex.Message);
                    receiveUdpClient.Close();
                    break;
                }
            }
        }

        public async Task ParseRequestAsync(UdpClient udpClient)
        {
            //Log("Begin parse request from client");
            var resultReceived = await udpClient.ReceiveAsync();
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
                        UserInfo newUser = new UserInfo(userName, ipEndPoint);
                        OnlineUsersList.Add(newUser);
                        await SendAcceptAsync(newUser);
                        await SendBroadcastAsync(request, ipEndPoint);
                        break;
                    case "logout":
                        Log(string.Format("User {0}[{1}] request to log out", userName, ipEndPoint));
                        for (int i = 0; i < OnlineUsersList.Count; i++)
                        {
                            if (OnlineUsersList[i].LocalIPEndPoint.Equals(ipEndPoint))
                            {
                                OnlineUsersList.RemoveAt(i);
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

        private async Task SendAcceptAsync(UserInfo User)
        {
            Log(string.Format("Accept user {0}[{1}]", User.UserName, User.LocalIPEndPoint));

            UdpClient udpClient = new UdpClient(0);
            Byte[] bytesSend = Encoding.UTF8.GetBytes("Accept," + tcpPort.ToString());
            string[] splitString = User.LocalIPEndPoint.Split(':');
            if(splitString.Length == 2)
            {
                string ip = splitString[0];
                int port = int.Parse(splitString[1]);
                Log(string.Format("Sending accept to user {0}:{1}", ip, port));
                IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                await udpClient.SendAsync(bytesSend, bytesSend.Length, remoteIPEndPoint);
                udpClient.Close();
            }
            else
            {
                Log("The user 'LocalIPEndPoint' format not correct.");
            }
            
        }

        private async Task SendBroadcastAsync(string Message, string ipEndPoint)
        {
            Log(string.Format("Broadcast [{0}] to the others", Message));

            UdpClient udpClient = new UdpClient(0);
            Byte[] bytesSend = Encoding.UTF8.GetBytes(Message);
            foreach (UserInfo user in OnlineUsersList)
            {
                if (!user.LocalIPEndPoint.Equals(ipEndPoint))
                {
                    string[] splitString = user.LocalIPEndPoint.Split(':');
                    IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Parse(splitString[0]), int.Parse(splitString[1]));
                    await udpClient.SendAsync(bytesSend, bytesSend.Length, remoteIPEndPoint);
                }
            }

            udpClient.Close();
        }

        private async Task SendOnlineUsersList()
        {
            string strUserList = "";
            //TcpClient newTcpClient = tcpClient;
            foreach(UserInfo user in OnlineUsersList)
            {
                strUserList += user.UserName + "," + user.LocalIPEndPoint + ";";
            }
            strUserList += "end";

            NetworkStream networkStream = tcpClient.GetStream();
            StreamWriter clientWriter = new StreamWriter(networkStream);
            clientWriter.AutoFlush = true;

            Log(string.Format("Send online userlist to [{0}]", tcpClient.Client.RemoteEndPoint));
            Log(string.Format("Online user list: [{0}]", strUserList));
            await clientWriter.WriteLineAsync(strUserList);

            clientWriter.Close();
            tcpClient.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
