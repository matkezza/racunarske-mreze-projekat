using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using HomeAppliances;

namespace User
{
    class Program
    {
        static void Main(string[] args)
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, 50001);

            byte[] buffer = new byte[4096];
            BinaryFormatter formatter = new BinaryFormatter();
            string odgovor = "";
            int assignedPort;

            try
            {
                clientSocket.Connect(serverEP);
                Console.WriteLine("Povezan na server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Doslo je do greske prilikom povezivanja na TCP server: " + ex.Message);
                Console.ReadLine();
                return;
            }


        LOGIN:
            Console.Clear();
            Console.WriteLine("==================== PAMETNA KUĆA | KORISNIK ====================");
            Console.WriteLine("Unesi kredencijale (format na serveru je username:password).\n");

            Console.Write("Username: ");
            string u = (Console.ReadLine() ?? "").Trim();
            Console.Write("Password: ");
            string p = (Console.ReadLine() ?? "").Trim();

            if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
            {
                Console.WriteLine("Username/password ne smeju biti prazni. Pritisni ENTER...");
                Console.ReadLine();
                goto LOGIN;
            }

            try
            {
                clientSocket.Send(Encoding.UTF8.GetBytes($"{u}:{p}"));
                int rb = clientSocket.Receive(buffer);
                odgovor = Encoding.UTF8.GetString(buffer, 0, rb).Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška pri loginu (TCP): " + ex.Message);
                Console.WriteLine("Pritisni ENTER da pokušaš ponovo...");
                Console.ReadLine();
                goto LOGIN;
            }

            if (!odgovor.StartsWith("USPESNO"))
            {
                Console.WriteLine("Login neuspešan.");
                Console.WriteLine("Pritisni ENTER da pokušaš ponovo...");
                Console.ReadLine();
                goto LOGIN;
            }

            int tmpPort;
            if (!int.TryParse(odgovor.Split(':')[1], out tmpPort))
            {
                Console.WriteLine("Server vratio neispravan UDP port. Pritisni ENTER...");
                Console.ReadLine();
                goto LOGIN;
            }
            assignedPort = tmpPort;
            Console.WriteLine("Login OK. UDP port: " + assignedPort);

            
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(new IPEndPoint(IPAddress.Any, 0)); 
            EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            IPEndPoint serverUdp = new IPEndPoint(IPAddress.Loopback, assignedPort);

            udpSocket.SendTo(Encoding.UTF8.GetBytes("HELLO"), serverUdp);

            List<Appliance> uredjaji = null;
            if (!TryReceiveDeviceList(udpSocket, formatter, ref ep, out uredjaji))
            {
                Console.WriteLine("Ne dobijam listu uređaja (UDP handshake). Pritisni ENTER za ponovni login...");
                SafeClose(udpSocket);
                Console.ReadLine();
                goto LOGIN;
            }

            
            object udpSendLock = new object();
           
            while (true)
            {
                Console.Clear();
                Console.WriteLine("==================== PAMETNA KUĆA | KORISNIK ====================");
                Console.WriteLine($"Active session: {u} | Server UDP: {assignedPort}\n");

                PrintDevices(uredjaji);

                int idx = ReadIntInRange("Choose a device (number): ", 1, uredjaji.Count) - 1;
                Appliance a = uredjaji[idx];

                Console.WriteLine();
                Console.WriteLine($"Device: {a.Name}");
                Console.WriteLine("Functions (current values):");
                foreach (var f in a.Functions)
                    Console.WriteLine($"- {f.Key} = {f.Value}");

                string func = ReadNonEmpty("Function: ");
                string val = ReadNonEmpty("New value: ");

                try
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        formatter.Serialize(ms, a);
                        formatter.Serialize(ms, func);
                        formatter.Serialize(ms, val);
                        lock (udpSendLock) { udpSocket.SendTo(ms.ToArray(), serverUdp); }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error occured (UDP): " + ex.Message);
                    Console.WriteLine("Tab an ENTER...");
                    Console.ReadLine();
                    continue;
                }

                string poruka;
                if (!TryReceiveText(udpSocket, ref ep, out poruka, timeoutMs: 4000))
                {
                    Console.WriteLine("No answer from server (timeout).\nTap an ENTER...");
                    Console.ReadLine();
                    continue;
                }

                if (poruka.Trim().Equals("SESSION_EXPIRED", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Session expired due inactivity. Tap an ENTER for new login..."); SafeClose(udpSocket);
                    Console.ReadLine();
                    goto LOGIN;
                }

                Console.WriteLine();
                Console.WriteLine("Answer from server:");
                Console.WriteLine(poruka);

                string dalje = ReadChoice("\nMore commands? (yes/no): ", "yes", "no");
                lock (udpSendLock) { udpSocket.SendTo(Encoding.UTF8.GetBytes(dalje), serverUdp); }

                if (dalje == "no")
                    break;

                if (!TryReceiveDeviceList(udpSocket, formatter, ref ep, out uredjaji))
                {
                    Console.WriteLine("Not receiving a list of devices (timeout). Tap an ENTER...");
                    Console.ReadLine();
                }
            }
            SafeClose(udpSocket);
            SafeClose(clientSocket);
        }

        private static void PrintDevices(List<Appliance> uredjaji)
        {
            Console.WriteLine("Devices:");
            Console.WriteLine(new string('-', 70));
            for (int i = 0; i < uredjaji.Count; i++)
            {
                var a = uredjaji[i];
                string state = string.Join(", ", a.Functions.Select(kv => $"{kv.Key}={kv.Value}"));
                if (state.Length > 45) state = state.Substring(0, 42) + "...";
                Console.WriteLine($"{i + 1,2}. {a.Name,-18} | port {a.Port,-5} | {state}");
            }
            Console.WriteLine(new string('-', 70));
        }

        private static bool TryReceiveDeviceList(Socket udp, BinaryFormatter formatter, ref EndPoint ep, out List<Appliance> devices)
        {
            devices = null;
            byte[] buf = new byte[65507];
            int bytes;
            if (!udp.Poll(3000 * 1000, SelectMode.SelectRead))
                return false;
            try
            {
                bytes = udp.ReceiveFrom(buf, ref ep);
            }
            catch
            {
                return false;
            }

            if (bytes <= 0) return false;

            try
            {
                using (MemoryStream ms = new MemoryStream(buf, 0, bytes))
                    devices = (List<Appliance>)formatter.Deserialize(ms);
                return devices != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReceiveText(Socket udp, ref EndPoint ep, out string text, int timeoutMs)
        {
            text = null;
            byte[] buf = new byte[65507];
            if (!udp.Poll(timeoutMs * 1000, SelectMode.SelectRead))
                return false;
            int bytes;
            try
            {
                bytes = udp.ReceiveFrom(buf, ref ep);
            }
            catch
            {
                return false;
            }
            if (bytes <= 0) return false;
            text = Encoding.UTF8.GetString(buf, 0, bytes);
            return true;
        }

        private static int ReadIntInRange(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                string s = (Console.ReadLine() ?? "").Trim();
                int v;
                if (int.TryParse(s, out v) && v >= min && v <= max)
                    return v;
                Console.WriteLine($"Unesi broj između {min} i {max}.");
            }
        }

        private static string ReadNonEmpty(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string s = (Console.ReadLine() ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(s)) return s;
                Console.WriteLine("Can't be empty.");
            }
        }

        private static string ReadChoice(string prompt, string a, string b)
        {
            while (true)
            {
                Console.Write(prompt);
                string s = (Console.ReadLine() ?? "").Trim().ToLower();
                if (s == a || s == b) return s;
                Console.WriteLine($"ENTER '{a}' or '{b}'.");
            }
        }

        private static void SafeClose(Socket s)
        {
            if (s == null) return;
            try { s.Shutdown(SocketShutdown.Both); } catch { }
            try { s.Close(); } catch { }
        }
    }
}