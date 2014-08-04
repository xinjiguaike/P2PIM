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
    public class User
    {
        public readonly string UserName;
        public readonly string LocalIpEndPoint;
    
        public User(string name, string endpoint)
        {
            UserName = name;
            LocalIpEndPoint = endpoint;
        }
    }
}
