using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PIM_Client
{
    public class PassDataWinEventArgs : EventArgs
    {
        public PassDataWinEventArgs(string localIpEndPoint)
        {
            LocalIpEndPoint = localIpEndPoint;
        }

        public string LocalIpEndPoint { get; set; }
    }
}
