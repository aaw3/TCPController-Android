using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

//using Android.App;
//using Android.Content;
//using Android.OS;
//using Android.Runtime;
//using Android.Views;
//using Android.Widget;

namespace TCPCommanderAndroid
{
    public class TCP_Connection
    {
        private static TCP_Connection _TCP_instance;
        public static TCP_Connection TCP_Instance
        {
            get
            {
                if (_TCP_instance == null) _TCP_instance = new TCP_Connection();
                return _TCP_instance;
            }
        }
        public TcpClient client { get; set; }
    }

    public class UDP_Connection
    {
        private static UDP_Connection _UDP_instance;
        public static UDP_Connection UDP_Instance
        {
            get
            {
                if (_UDP_instance == null) _UDP_instance = new UDP_Connection();
                return _UDP_instance;
            }
        }
        public UdpClient client { get; set; }
    }
}