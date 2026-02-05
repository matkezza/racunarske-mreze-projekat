using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UserLibrary;
using HomeAppliances;

namespace CentralServer
{
    public class Server
    {
        TcpListener listerner;
        UserLibrary.ClientL client;
        List<Appliance> appliances;
        List<string> logs = new List<string>();
        int nextUdpPort = 6000;

        
    }
}
