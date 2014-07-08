using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace P2PIM_Client
{
    public class User: INotifyPropertyChanged
    {
        private string _name;
        private IPAddress _ip;
        private int _port;
        private bool _isLoggedin;

        private string _msgRecord;

        public User()
        {

        }
    }
}
