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
    public class User : INotifyPropertyChanged
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

        private string _localIpEndPoint;
        public string LocalIpEndPoint
        {
            get { return _localIpEndPoint; }
            set
            {
                _localIpEndPoint = value;
                OnPropertyChanged("LocalIpEndPoint");
            }
        }

        public User(string name, string ipEndPoint)
        {
            UserName = name;
            LocalIpEndPoint = ipEndPoint;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
