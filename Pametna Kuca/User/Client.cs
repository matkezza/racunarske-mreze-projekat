/*using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using HomeAppliances;

namespace User
{
    public class Program
    {
        static void Main(string[] args)
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, 50001);
            byte[] buffer = new byte[4096];
            BinaryFormatter formatter = new BinaryFormatter();
            string odgovor = "";
            int brojBajta = 0;
            int assignedPort = -1;
            try
            {
                clientSocket.Connect(serverEP);
                Console.WriteLine("Klijent je uspešno povezan sa serverom!");
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.WouldBlock)
                {
                    Console.WriteLine("Povezivanje u toku...");
                }
                else
                {
                    Console.WriteLine("Greška prilikom povezivanja: " + ex.Message);
                    return;
                }
            }
        PONOVNO_LOGOVANJE:
            do
            {
                Console.WriteLine("Enter an username: ");
                string username = Console.ReadLine();

                Console.WriteLine("Enter a password: ");
                string password = Console.ReadLine();

                string format = $"{username}:{password}";
                brojBajta = clientSocket.Send(Encoding.UTF8.GetBytes(format));

                clientSocket.Blocking = false;

                if (clientSocket.Poll(1000 * 1000, SelectMode.SelectRead))
                {
                    brojBajta = clientSocket.Receive(buffer);
                    odgovor = Encoding.UTF8.GetString(buffer, 0, brojBajta).Trim();
                    clientSocket.Blocking = true;
                }

                string[] parts = odgovor.Split(':');

                if (parts[0] != "USPESNO")
                {
                    Console.WriteLine("Login neuspešan");
                    goto PONOVNO_LOGOVANJE;
                }

                assignedPort = int.Parse(parts[1]);
                Console.WriteLine("UDP port dodeljen: " + assignedPort);


            }
            while (odgovor != "USPESNO");

            

            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));


            EndPoint deviceEndPoint = new IPEndPoint(IPAddress.Any, 0);
            
            IPEndPoint destinationEP = new IPEndPoint(IPAddress.Loopback, assignedPort);

           

            string initialMessage = $"Klijent se povezao na UDP port: {assignedPort}";
            byte[] initialData = Encoding.UTF8.GetBytes(initialMessage);
            
            
            udpSocket.SendTo(initialData, destinationEP);
            Console.WriteLine($"Poruka poslata serveru: {initialMessage}");

            brojBajta = udpSocket.ReceiveFrom(buffer, ref deviceEndPoint);


            List<Appliance> uredjaji;
            using (MemoryStream ms = new MemoryStream(buffer, 0, brojBajta))
            {
                uredjaji = (List<Appliance>)formatter.Deserialize(ms);
            }


            try
            {
                while (true)
                {
                    brojBajta = udpSocket.ReceiveFrom(buffer, ref deviceEndPoint);
                    if (brojBajta > 0)
                    {

                        using (MemoryStream ms = new MemoryStream(buffer, 0, brojBajta))
                        {
                            uredjaji = (List<Appliance>)formatter.Deserialize(ms);
                        }

                        Console.WriteLine("Lista dostupnih uređaja:");
                        for (int i = 0; i < uredjaji.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {uredjaji[i].Name} (Port: {uredjaji[i].Port})");
                        }

                        int izbor;
                        do
                        {
                            Console.WriteLine("Unesite redni broj uređaja koji želite da podesite:");
                            izbor = Int32.Parse(Console.ReadLine()) - 1;
                        } while (izbor < 0 || izbor >= uredjaji.Count);

                        Appliance izabraniUredjaj = uredjaji[izbor];
                        Console.WriteLine($"Izabrali ste uređaj: {izabraniUredjaj.Name}");
                        Console.WriteLine("[Ime funkcije, Vrednost]");
                        Console.WriteLine("--------------------------");
                        foreach (var v in izabraniUredjaj.Functions)
                        {
                            Console.WriteLine("-" + v.ToString());
                        }

                        bool provera = false;
                        string vrednost = "";
                        string funkcija = "";
                        do
                        {
                            Console.WriteLine("Unesite ime funkcije koju želite da promenite:");
                            funkcija = Console.ReadLine().Trim();
                            foreach (var f in izabraniUredjaj.Functions)
                            {
                                if (f.Key == funkcija)
                                {
                                    provera = true;
                                    break;
                                }
                            }
                            if (provera)
                            {
                                Console.WriteLine("Unesite novu vrednost:");
                                vrednost = Console.ReadLine();
                            }
                        } while (!provera);

                        initialData = Encoding.UTF8.GetBytes($"{izabraniUredjaj.Name}:{funkcija}:{vrednost}");
                        using (MemoryStream ms = new MemoryStream())
                        {
                            BinaryFormatter bf = new BinaryFormatter();
                            bf.Serialize(ms, izabraniUredjaj);
                            bf.Serialize(ms, funkcija);
                            bf.Serialize(ms, vrednost);
                            byte[] data = ms.ToArray();

                            udpSocket.SendTo(data, destinationEP);
                        }
                        int receivedBytes = udpSocket.ReceiveFrom(buffer, ref deviceEndPoint);
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                        Console.WriteLine($"Server preko UDP je poslao poruku-> {receivedMessage}");

                        string dodatak = "";
                        do
                        {
                            Console.WriteLine(receivedMessage + "(da/ne)");
                            dodatak = Console.ReadLine();
                        } while (dodatak != "da" && dodatak != "ne");

                        if (dodatak.ToLower() == "ne")
                        {
                            udpSocket.SendTo(Encoding.UTF8.GetBytes(dodatak), destinationEP);
                            break;
                        }
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(dodatak), destinationEP);
                    }

                }
            }
            catch (SocketException ex)
            {
                
                Console.WriteLine("Sesija je istekla ili je port zatvoren. Molimo vas da se ponovo prijavite.");
                udpSocket.Close();
                goto PONOVNO_LOGOVANJE;  
            }
            catch (Exception ex)
            {
                Console.WriteLine("Neočekivana greška: " + ex.Message);
            }

            Console.WriteLine("Klijent završava sa radom.");
        Console.ReadKey();
        clientSocket.Close();



        }
    }
}*/

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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

            clientSocket.Connect(serverEP);
            Console.WriteLine("Povezan na server.");

            try
            {
                clientSocket.Connect(serverEP);
                Console.WriteLine("Povezan na server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Doslo je do greske prilikom povezivanja na TCP server: " + ex.Message);
                return;
            }

        LOGIN:
            Console.Clear();
            Console.WriteLine("============================= PAMETNA KUCA | KORISNIK ================================ ");
            Console.WriteLine("Unesi kredencijale(format na serveru je username:password).\n");

            Console.WriteLine("Username:");
            string u = (Console.ReadLine() ?? "").Trim();
            Console.WriteLine("Password:");
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
                Console.WriteLine("Greska pri loginu (TCP): " + ex.Message);
                Console.WriteLine("Pritisni ENTER da pokusas ponovo...");
                Console.ReadLine();
                goto LOGIN;
            }

            if (!odgovor.StartsWith("USPESNO"))
            {
                Console.WriteLine("Login neuspesan.\nPritisni ENTER da pokusas ponovo...");
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
                Console.WriteLine("Ne dobijam listu uredjaja (UDP handshake). Pritisni ENTER za ponovni login...");
                SafeClose(udpSocket);

                Console.ReadLine();
                goto LOGIN;
            }

            object udpSendLock = new object();

            while(true)
            {
                Console.Clear();
                Console.WriteLine("==================== PAMETNA KUĆA | KORISNIK ====================");
                Console.WriteLine($"Aktivna sesija: {u} | Server UDP: {assignedPort}\n");

                PrintDevices(uredjaji);
                int idx = ReadIntInRange("Izaberi uredaj: ",1,uredjaji.Count)-1;
                Appliance a = uredjaji[idx];

                Console.WriteLine();
                Console.WriteLine($"Uredjaj: {a.Name}");
                Console.WriteLine("Funkcije (trenutne vrednosti):");
                foreach(var f in a.Functions)
                    Console.WriteLine($"-{f.Key} = {f.Value}");

                string func = ReadNonEmpty("Funkcija: ");
                string val = ReadNonEmpty("Nova vrednost: ");

                try 
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        formatter.Serialize(ms, a);
                        formatter.Serialize(ms, func);
                        formatter.Serialize(ms, val);
                        lock (udpSendLock) { udpSocket.SendTo(ms.ToArray(), serverUdp);}

                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Greska pri slanju komande: " + ex.Message);
                    Console.WriteLine("Pritisni ENTER...");
                    Console.ReadLine();
                    continue;
                }

                string poruka;
                if(!TryReceiveText(udpSocket,ref ep, out poruka, timeoutMs:4000))
                {
                    Console.WriteLine("Nema odgovora od servera.\nPritisnite ENTER...");
                    Console.ReadLine();
                    continue;
                }

                if (poruka.Trim().Equals("SESSION_EXPIRED", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Sesija je istekla zbog neaktivnosti. Pritisni ENTER za ponovni login.");
                    SafeClose(udpSocket);
                    Console.ReadLine();
                    goto LOGIN;
                }

                Console.WriteLine();
                Console.WriteLine("Odgovor servera: ");
                Console.WriteLine(poruka);

                string dalje = ReadChoice("\nJos komandi? (da/ne):","da","ne");
                lock (udpSendLock) { udpSocket.SendTo(Encoding.UTF8.GetBytes(dalje), serverUdp); }

                if(dalje == "ne")
                {
                    break;
                }

                if(!TryReceiveDeviceList(udpSocket,formatter,ref ep, out uredjaji))
                {
                    Console.WriteLine("Ne dobijam listu uredjaja. Pritisnite ENTER...");
                    Console.ReadLine();

                }
                SafeClose(udpSocket);
                SafeClose(clientSocket);
            }

        }


        private static void PrintDevices(List<Appliance> uredjaji)
        {
            Console.WriteLine("Uređaji:");
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
                Console.WriteLine("Ne sme biti prazno.");
            }
        }

        private static string ReadChoice(string prompt, string a, string b)
        {
            while (true)
            {
                Console.Write(prompt);
                string s = (Console.ReadLine() ?? "").Trim().ToLower();
                if (s == a || s == b) return s;
                Console.WriteLine($"Unesi '{a}' ili '{b}'.");
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
