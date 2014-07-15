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

namespace P2PIM_Client
{
    public class UserInfo : INotifyPropertyChanged
    {
        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                OnPropertyChanged("UserName");
            }
        }

        private string _localIPEndPoint;
        public string LocalIPEndPoint
        {
            get { return _localIPEndPoint; }
            set
            {
                _localIPEndPoint = value;
                OnPropertyChanged("LocalIPEndPoint");
            }
        }

        public UserInfo(string Name, string IPEndPoint)
        {
            UserName = Name;
            LocalIPEndPoint = IPEndPoint;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class User: INotifyPropertyChanged
    {
        public bool IsOnline;
        public UserInfo ObjChatTo;
        public ObservableCollection<UserInfo> OnlineUsersList;

        private string _name;
        public string Name
        {
            get 
            { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged("Name");
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


        public User()
        {
            IsOnline = false;
            Name = "Rudy";
            LocalIP = "192.168.0.10";
            LocalPort = 8080;
            ServerIP = "10.224.202.82";
            ServerPort = 65535;
            MsgRecord = "Hello";
            OnlineUsersList = new ObservableCollection<UserInfo>();
            OnlineUsersList.Add(new UserInfo("Rudy", "10.224.202.82:6023"));
            OnlineUsersList.Add(new UserInfo("Home", "192.168.0.1:4355"));
            OnlineUsersList.Add(new UserInfo("Office", "173.39.17.56:8080"));
        }

        public async Task LoginAsync()
        {
            await SendLogInOutMessageAsync("login");

        }


        public async Task SendLogInOutMessageAsync(string actionType)
        {
            string message = string.Format("{0},{1},{2}:{3}", actionType, Name, LocalIP, LocalPort);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(ServerIP), ServerPort);
            UdpClient sendUdpClient = new UdpClient(remoteEndPoint);
            Byte[] bytesSend = Encoding.UTF8.GetBytes(message);
            await sendUdpClient.SendAsync(bytesSend, bytesSend.Length);
            sendUdpClient.Close();
        }

        public async Task SendChatMessageAsync()
        {
            string[] splitString = ObjChatTo.LocalIPEndPoint.Split(':');
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(splitString[0]), int.Parse(splitString[1]));
            UdpClient sendUdpClient = new UdpClient(remoteEndPoint);
            Byte[] bytesSend = Encoding.UTF8.GetBytes(string.Format("chat,{0},{1}", DateTime.Now, ChatContent));
            await sendUdpClient.SendAsync(bytesSend, bytesSend.Length);
            sendUdpClient.Close();
        }

        public void ReceiveMessage()
        {
            IPEndPoint remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
            UdpClient receiveUdpClient = new UdpClient(remoteIPEndPoint);
            while (true)
            {
                try
                {
                    new Action(async () => await ParseResponseAsync(receiveUdpClient))();
                }
                catch
                {
                    receiveUdpClient.Close();
                    break;
                }
            }

            
        }

        public async Task ParseResponseAsync(UdpClient udpClient)
        {
            var resultReceived = await udpClient.ReceiveAsync();
            string request = Encoding.UTF8.GetString(resultReceived.Buffer);

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
                            TcpClient tcpClient = new TcpClient();
                            tcpClient.Connect(ServerIP, int.Parse(splitString[1]));
                            NetworkStream networkStream = tcpClient.GetStream();
                            StreamReader reader = new StreamReader(networkStream);
                            await GetOnlineUsersList(reader);
                        }
                        catch(Exception ex)
                        {
                            Trace.TraceInformation("Tcp connect failed[Exception: {0}]", ex.Message);
                        }
                        break;
                    case "login":
                        Trace.TraceInformation(string.Format("User {0}[{1}] join", userName, ipEndPoint));
                        OnlineUsersList.Add(new UserInfo(userName, ipEndPoint));
                        break;
                    case "logout":
                        for (int i = 0; i < OnlineUsersList.Count; i++)
                        {
                            if (OnlineUsersList[i].UserName == userName)
                            {
                                OnlineUsersList.RemoveAt(i);
                                break;
                            }
                        }

                        Trace.TraceInformation(string.Format("User {0}[{1}] exit", userName, ipEndPoint));
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

        public async Task GetOnlineUsersList(StreamReader readerStream)
        {
            while(true)
            {
                try
                {
                    string resultReceived = await readerStream.ReadToEndAsync();
                    if (resultReceived.EndsWith("end"))
                    {
                        string[] splitString = resultReceived.Split(';');
                        for (int i = 0; i < splitString.Length - 1; i++)
                        {
                            string[] userString = splitString[i].Split(',');
                            UserInfo newUser = new UserInfo(userString[0], userString[1]);
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
