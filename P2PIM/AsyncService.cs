using P2PIM.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace P2PIM
{
    public class AsyncService: INotifyPropertyChanged
    {
        private int port;
        private IPAddress serverIP;
        private TcpListener listener;
        private TcpClient tcpClient;
        private TcpClient client;
        private StreamWriter clientWriter;



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

        private string _messageToSend;
        public string MessageToSend
        {
            get { return _messageToSend; }
            set
            {
                _messageToSend = value;
                OnPropertyChanged("MessageToSend");
            }
        }

        public AsyncService()
        {
            port = 5000;
        }

        public async Task StartListenAsync()
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            try
            {
                while(true)
                {
                    tcpClient = await listener.AcceptTcpClientAsync();
                    await ReceiveChatMessageAsync();
                }     
            }
            catch(Exception e)
            {
                Trace.TraceError("Rudy Trace => Listen Exception: {0}", e.Message);
            }
            
        }

        public async Task StartConnectAsync()
        {
            client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(Settings.Default.TargetIP), port);
            NetworkStream networkStream = client.GetStream();
            clientWriter = new StreamWriter(networkStream);
            clientWriter.AutoFlush = true;
        }

        public void StopListen()
        {
            if(listener != null)
            listener.Stop();
        }

        public void StopConnect()
        {
            if (client != null)
            {
                if (client.Connected)
                    client.Close();
            }
            if (tcpClient != null)
            {
                if (tcpClient.Connected)
                    tcpClient.Close();
            }
        }

        public async Task ReceiveChatMessageAsync()
        {
            try
            {
                string clientEndPoint = tcpClient.Client.RemoteEndPoint.ToString();
                NetworkStream networkStream = tcpClient.GetStream();
                StreamReader reader = new StreamReader(networkStream);
                while (true)
                {
                    string request = await reader.ReadLineAsync();
                    if (request != null)
                    {
                        Trace.TraceInformation("Rudy Trace => Received service request: " + request);
                        ChatContent += request + "--[" + clientEndPoint + "]\n";
                        Trace.TraceInformation("Rudy Trace => Computed response is: " + ChatContent);
                    }
                    else
                    {
                        Trace.TraceInformation("Rudy Trace => Client closed the connection.");
                        break; 
                    }
                }
                
                tcpClient.Close();
            }
            catch (Exception e)
            {
                Trace.TraceError("Rudy Trace => ShowChatMessage Exception: {0}", e.Message);
                if (tcpClient.Connected)
                    tcpClient.Close();
            }

        }


        public async Task SendMessageAsync()
        {
            try
            {
                await StartConnectAsync();

                Trace.TraceInformation("Rudy Trace => SendMessageAsync Message: {0}", MessageToSend);
                if (MessageToSend == "")
                {
                    MessageBox.Show("You can not send the empty message!");
                    return;
                }

                await clientWriter.WriteLineAsync(MessageToSend);
                client.Close();
            }
            catch (Exception e)
            {
                Trace.TraceError("Rudy Trace => SendMessageAsync Exception: {0}", e.Message);
                if (client.Connected)
                    client.Close();
            } 
        }

        

        public async Task SetServerIpAsync()
        {
            string hostName = Dns.GetHostName();
            IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync(hostName);

            foreach (IPAddress ip in ipHostInfo.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    serverIP = ip;
                    break;
                }
            }
            throw new Exception("No IPv4 address for server");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string PropertyName)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
            }
        }
    }
}
