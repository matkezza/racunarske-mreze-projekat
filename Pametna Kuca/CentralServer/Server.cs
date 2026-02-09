using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using HomeAppliances;
using UserLibrary;


namespace CentralServer
{
    public class Server
    {
        private Socket _listenTcp;
        private readonly List<Socket> _tcpClients = new List<Socket>();
        private readonly Dictionary<Socket, string> _pendingUsername = new Dictionary<Socket, string>();
        private readonly Dictionary<int, Session> _sessionsByUdpPort = new Dictionary<int, Session>();

        private readonly List<ClientL> _users = new List<ClientL>();
        private readonly List<Appliance> _appliances = new List<Appliance>();
        private readonly List<string> _logs = new List<string>();
        private const int MaxLogsKept = 500;
        private const int LogsShown = 15;

        private int _nextUdpPort = 6000;
        private readonly int _tcpPort;
        private readonly int _inactivityCyclesToClose;
        private readonly int _cycleMs;

        private readonly BinaryFormatter _bf = new BinaryFormatter();

        public static void Main(string[] args)
        {
            int tcpPort = 50001;
            int inactivityCycles = 1800; 
            int cycleMs = 1000;

            Server s = new Server(tcpPort, inactivityCycles, cycleMs);
            s.Run();
        }

        public Server(int tcpPort, int inactivityCyclesToClose, int cycleMs)
        {
            _tcpPort = tcpPort;
            _inactivityCyclesToClose = inactivityCyclesToClose;
            _cycleMs = cycleMs;

            SeedUsers();
            SeedAppliances();
        }

        private void SeedUsers()
        {

            _users.Add(new ClientL("Matija", "Brkusanin", "matija", "1234", false, 0));
            _users.Add(new ClientL("Luka", "Kukic", "luka", "1234", false, 0));
            _users.Add(new ClientL("Test", "User", "test", "test", false, 0));
        }

        private void SeedAppliances()
        {
            Appliance seed = new Appliance();
            _appliances.Clear();
            _appliances.AddRange(seed.ListOfAppliances());
        }

        public void Run()
        {
            Console.OutputEncoding = Encoding.UTF8;

            _listenTcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenTcp.Bind(new IPEndPoint(IPAddress.Any, _tcpPort));
            _listenTcp.Listen(50);
            _listenTcp.Blocking = false;

            Log($"CentralServer started. TCP listening on {_tcpPort}.");

            while (true)
            {
                try
                {
                    List<Socket> checkRead = new List<Socket>();
                    List<Socket> checkError = new List<Socket>();

                    checkRead.Add(_listenTcp);
                    checkError.Add(_listenTcp);

                    foreach (var c in _tcpClients.ToList())
                    {
                        checkRead.Add(c);
                        checkError.Add(c);
                    }

                    foreach (var sess in _sessionsByUdpPort.Values.ToList())
                    {
                        checkRead.Add(sess.UdpSocket);
                        checkError.Add(sess.UdpSocket);
                    }

                    
                    Socket.Select(checkRead, null, checkError, _cycleMs * 1000);

                    if (checkError.Count > 0)
                    {
                        foreach (var sock in checkError.ToList())
                        {
                            try
                            {
                                if (sock == _listenTcp)
                                {
                                    Log("TCP listen socket error.");
                                    continue;
                                }

                                if (_tcpClients.Contains(sock))
                                {
                                    CloseTcpClient(sock, "Socket error");
                                    continue;
                                }

                                var sessionErr = _sessionsByUdpPort.Values.FirstOrDefault(x => x.UdpSocket == sock);
                                if (sessionErr != null)
                                {
                                    CloseSession(sessionErr, "UDP socket error");
                                    continue;
                                }
                            }
                            catch { }
                        }
                    }

                    if (checkRead.Count > 0)
                    {
                        foreach (var sock in checkRead.ToList())
                        {
                            if (sock == _listenTcp)
                            {
                                AcceptTcpClients();
                                continue;
                            }

                            if (_tcpClients.Contains(sock))
                            {
                                ReceiveTcpFromClient(sock);
                                continue;
                            }

                            var session = _sessionsByUdpPort.Values.FirstOrDefault(x => x.UdpSocket == sock);
                            if (session != null)
                            {
                                HandleUdp(session);
                                continue;
                            }
                        }
                    }

                    TickInactivity();
                    RenderStatus();
                }
                catch (Exception ex)
                {
                    Log($"FATAL(loop): {ex.GetType().Name}: {ex.Message}");
                    Thread.Sleep(200);
                }
            }
        }

        private void AcceptTcpClients()
        {
            while (true)
            {
                try
                {
                    Socket c = _listenTcp.Accept();
                    c.Blocking = false;
                    _tcpClients.Add(c);
                    Log($"TCP client connected: {SafeEP(c.RemoteEndPoint)}");
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock)
                        break;
                    Log($"TCP accept error: {ex.SocketErrorCode} {ex.Message}");
                    break;
                }
            }
        }
        private void ReceiveTcpFromClient(Socket clientSocket) => HandleTcp(clientSocket);
        private void CloseTcpClient(Socket clientSocket, string reason) => CloseTcp(clientSocket, reason);

        private void HandleTcp(Socket clientSocket)
        {
            byte[] buffer = new byte[4096];
            int bytes = 0;
            try
            {
                bytes = clientSocket.Receive(buffer);
                if (bytes <= 0)
                {
                    CloseTcp(clientSocket, "TCP closed by peer");
                    return;
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.WouldBlock)
                    return;

                CloseTcp(clientSocket, $"TCP receive error: {ex.SocketErrorCode}");
                return;
            }

            string msg = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();
            if (string.IsNullOrWhiteSpace(msg))
                return;

            string[] parts = msg.Split(':');
            if (parts.Length != 2)
            {
                SendTcp(clientSocket, "NEUSPESNO");
                return;
            }

            string username = parts[0];
            string password = parts[1];

            var user = new ClientL().FindClient(_users, username, password);
            if (user == null)
            {
                SendTcp(clientSocket, "NEUSPESNO");
                Log($"Login failed for '{username}' from {SafeEP(clientSocket.RemoteEndPoint)}");
                return;
            }

            if (user.Status)
            {
                SendTcp(clientSocket, "NEUSPESNO");
                Log($"Login rejected (already active) for '{username}'");
                return;
            }

            int udpPort = AllocateUdpPort();
            user.Port = udpPort;
            user.Status = true;

            Socket udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udp.Bind(new IPEndPoint(IPAddress.Any, udpPort));
            udp.Blocking = false;

            var session = new Session
            {
                User = user,
                TcpSocket = clientSocket,
                UdpSocket = udp,
                UdpPort = udpPort,
                InactiveCycles = 0,
                ClientUdpEndpoint = null,
                State = SessionState.WaitingForUdpHandshake
            };
            _sessionsByUdpPort[udpPort] = session;

            SendTcp(clientSocket, $"USPESNO:{udpPort}");


            Log($"Login OK for '{username}'. Assigned UDP port {udpPort}.");
        }

        private void HandleUdp(Session session)
        {
            byte[] buffer = new byte[8192];
            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

            int bytes;
            try
            {
                bytes = session.UdpSocket.ReceiveFrom(buffer, ref remote);
                if (bytes <= 0) return;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.WouldBlock)
                    return;
                Log($"UDP error on {session.UdpPort}: {ex.SocketErrorCode}");
                return;
            }

            session.InactiveCycles = 0;

            string asText = null;
            try
            {
                asText = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();
            }
            catch { }

            if (session.ClientUdpEndpoint == null)
            {
                session.ClientUdpEndpoint = (IPEndPoint)remote;
                session.State = SessionState.Active;
                Log($"UDP handshake for '{session.User.Username}' on {session.UdpPort} from {session.ClientUdpEndpoint}");
                SendApplianceList(session);
                return;
            }

            if (!string.IsNullOrEmpty(asText) && asText.Equals("PING", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!string.IsNullOrEmpty(asText) && (asText.Equals("yes", StringComparison.OrdinalIgnoreCase)))
            {
                SendApplianceList(session);
                return;
            }

            if (!string.IsNullOrEmpty(asText) && (asText.Equals("no", StringComparison.OrdinalIgnoreCase)))
            {
                SendUdpText(session, "Server is done with work.");
                CloseSession(session, "Client requested logout");
                return;
            }

            int remotePort = ((IPEndPoint)remote).Port;
            var fromDevice = _appliances.FirstOrDefault(a => a.Port == remotePort);
            if (fromDevice != null)
            {
                string deviceFeedback = asText ?? "(device feedback)";
                Log($"Device '{fromDevice.Name}' -> {deviceFeedback}");
                SendUdpText(session, deviceFeedback);
                return;
            }

            try
            {
                Appliance targetAppliance;
                string funkcija;
                string vrednost;

                using (MemoryStream ms = new MemoryStream(buffer, 0, bytes))
                {
                    targetAppliance = (Appliance)_bf.Deserialize(ms);
                    funkcija = (string)_bf.Deserialize(ms);
                    vrednost = (string)_bf.Deserialize(ms);
                }

                var serverApp = _appliances.FirstOrDefault(a => a.Name == targetAppliance.Name);
                if (serverApp == null)
                {
                    SendUdpText(session, "Greška: uređaj ne postoji.");
                    return;
                }

                serverApp.AddCommand(funkcija, vrednost);
                Log($"CMD '{session.User.Username}' -> {serverApp.Name}: {funkcija} = {vrednost}");

                IPEndPoint deviceEP = new IPEndPoint(IPAddress.Loopback, serverApp.Port);
                string forward = $"{serverApp.Name}:{funkcija}:{vrednost}";
                byte[] forwardBytes = Encoding.UTF8.GetBytes(forward);

                session.UdpSocket.SendTo(forwardBytes, deviceEP);

                string response = WaitForDeviceResponse(session, serverApp.Port, maxWaitMs: 800);
                if (!string.IsNullOrWhiteSpace(response))
                {
                    SendUdpText(session, response);
                }
                else
                {
                    SendUdpText(session, $"Komanda prosleđena uređaju '{serverApp.Name}'.");
                }
            }
            catch (Exception ex)
            {
                Log($"UDP parse error on {session.UdpPort}: {ex.Message}");
                SendUdpText(session, "Greška: neispravan format komande.");
            }
        }

        private string WaitForDeviceResponse(Session session, int devicePort, int maxWaitMs)
        {
            DateTime end = DateTime.UtcNow.AddMilliseconds(maxWaitMs);
            byte[] buf = new byte[4096];

            while (DateTime.UtcNow < end)
            {
                if (!session.UdpSocket.Poll(100 * 1000, SelectMode.SelectRead))
                    continue;

                EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                int bytes = 0;
                try
                {
                    bytes = session.UdpSocket.ReceiveFrom(buf, ref remote);
                }
                catch
                {
                    continue;
                }

                if (bytes <= 0) continue;

                int port = ((IPEndPoint)remote).Port;
                if (port != devicePort)
                {
                    continue;
                }

                return Encoding.UTF8.GetString(buf, 0, bytes).Trim();
            }

            return null;
        }

        private void SendApplianceList(Session session)
        {
            try
            {
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    _bf.Serialize(ms, _appliances);
                    data = ms.ToArray();
                }

                session.UdpSocket.SendTo(data, session.ClientUdpEndpoint);
                Log($"Sent device list to '{session.User.Username}' ({session.ClientUdpEndpoint}).");
            }
            catch (Exception ex)
            {
                Log($"Failed to send device list: {ex.Message}");
            }
        }

        private void SendTcp(Socket c, string text)
        {
            try
            {
                byte[] b = Encoding.UTF8.GetBytes(text);
                c.Send(b);
            }
            catch { }
        }

        private void SendUdpText(Session session, string text)
        {
            if (session.ClientUdpEndpoint == null) return;
            try
            {
                byte[] b = Encoding.UTF8.GetBytes(text);
                session.UdpSocket.SendTo(b, session.ClientUdpEndpoint);
            }
            catch { }
        }

        private void TickInactivity()
        {
            foreach (var s in _sessionsByUdpPort.Values.ToList())
            {
                if (s.State != SessionState.Active) continue;

                s.InactiveCycles++;
                if (s.InactiveCycles >= _inactivityCyclesToClose)
                {
                    SendUdpText(s, "SESSION_EXPIRED");
                    CloseSession(s, "Session expired (inactivity)");
                }
            }
        }

        private void RenderStatus()
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================== PAMETNA KUĆA | CENTRAL SERVER ====================");
            Console.ResetColor();
            Console.WriteLine($"TCP: {_tcpPort} | TCP clients: {_tcpClients.Count} | Sessions: {_sessionsByUdpPort.Count} | Tick: {_cycleMs}ms");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[ACTIVE SESSIONS]");
            Console.ResetColor();
            Console.WriteLine(string.Format("{0,-12} {1,-6} {2,-26} {3,-6} {4,-10}", "Username", "UDP", "ClientEP", "Idle", "State"));
            Console.WriteLine(new string('-', 70));
            foreach (var s in _sessionsByUdpPort.Values.OrderBy(v => v.User.Username))
            {
                string ep = s.ClientUdpEndpoint != null ? s.ClientUdpEndpoint.ToString() : "(no udp yet)";
                Console.WriteLine(string.Format("{0,-12} {1,-6} {2,-26} {3,-6} {4,-10}",
                    s.User.Username, s.UdpPort, ep, s.InactiveCycles, s.State));
            }

            Console.WriteLine();

            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[DEVICES | STATES]");
            Console.ResetColor();
            Console.WriteLine(string.Format("{0,-18} {1,-6} {2,-50} {3,-19}", "Name", "Port", "Functions", "LastChange"));
            Console.WriteLine(new string('-', 100));
            foreach (var a in _appliances.OrderBy(x => x.Port))
            {
                string funcs = string.Join(", ", a.Functions.Select(f => $"{f.Key}={f.Value}"));
                if (funcs.Length > 50) funcs = funcs.Substring(0, 47) + "...";
                Console.WriteLine(string.Format("{0,-18} {1,-6} {2,-50} {3,-19}", a.Name, a.Port, funcs, a.LastChange.ToString("yyyy-MM-dd HH:mm:ss")));
            }

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[LOGS | LAST MESSAGES]");
            Console.ResetColor();
            Console.WriteLine(new string('-', 100));
            foreach (var l in _logs.Skip(Math.Max(0, _logs.Count - LogsShown)))
            {
                Console.WriteLine(l);
            }
        }

        private void Log(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            _logs.Add(line);
            if (_logs.Count > MaxLogsKept) _logs.RemoveAt(0);
        }

        private void CloseTcp(Socket c, string reason)
        {
            try { Log($"{reason}: {SafeEP(c.RemoteEndPoint)}"); } catch { }
            try { c.Shutdown(SocketShutdown.Both); } catch { }
            try { c.Close(); } catch { }
            _tcpClients.Remove(c);
            _pendingUsername.Remove(c);
        }

        private void CloseSession(Session s, string reason)
        {
            Log($"Session '{s.User.Username}' closed: {reason}");

            s.User.Status = false;
            s.User.Port = 0;

            try { s.UdpSocket.Close(); } catch { }

            try { CloseTcp(s.TcpSocket, "TCP closed (session end)"); } catch { }

            _sessionsByUdpPort.Remove(s.UdpPort);
        }

        private int AllocateUdpPort()
        {
            while (_sessionsByUdpPort.ContainsKey(_nextUdpPort))
                _nextUdpPort++;
            return _nextUdpPort++;
        }

        private static string SafeEP(EndPoint ep)
        {
            try { return ep?.ToString() ?? "(null)"; } catch { return "(?)"; }
        }

        private class Session
        {
            public ClientL User;
            public Socket TcpSocket;
            public Socket UdpSocket;
            public int UdpPort;
            public IPEndPoint ClientUdpEndpoint;
            public int InactiveCycles;
            public SessionState State;
        }

        private enum SessionState
        {
            WaitingForUdpHandshake,
            Active
        }
    }
}