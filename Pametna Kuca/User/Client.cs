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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
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

        LOGIN:
            Console.Write("Username: ");
            string u = Console.ReadLine();
            Console.Write("Password: ");
            string p = Console.ReadLine();

            clientSocket.Send(Encoding.UTF8.GetBytes($"{u}:{p}"));
            int rb = clientSocket.Receive(buffer);
            odgovor = Encoding.UTF8.GetString(buffer, 0, rb);

            if (!odgovor.StartsWith("USPESNO"))
            {
                Console.WriteLine("Login neuspešan.");
                goto LOGIN;
            }

            assignedPort = int.Parse(odgovor.Split(':')[1]);
            Console.WriteLine("Login OK. UDP port: " + assignedPort);

            // UDP
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
            EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            IPEndPoint serverUdp = new IPEndPoint(IPAddress.Loopback, assignedPort);

            // javi se serveru
            udpSocket.SendTo(Encoding.UTF8.GetBytes("HELLO"), serverUdp);

            // === PRVI I JEDINI PRIJEM LISTE ===
            rb = udpSocket.ReceiveFrom(buffer, ref ep);
            List<Appliance> uredjaji;
            using (MemoryStream ms = new MemoryStream(buffer, 0, rb))
                uredjaji = (List<Appliance>)formatter.Deserialize(ms);

            // === MENI ===
            while (true)
            {
                Console.WriteLine("\nUređaji:");
                for (int i = 0; i < uredjaji.Count; i++)
                    Console.WriteLine($"{i + 1}. {uredjaji[i].Name}");

                Console.Write("Izaberi uređaj: ");
                int idx = int.Parse(Console.ReadLine()) - 1;
                Appliance a = uredjaji[idx];

                Console.WriteLine("Funkcije:");
                foreach (var f in a.Functions)
                    Console.WriteLine($"{f.Key} = {f.Value}");

                Console.Write("Funkcija: ");
                string func = Console.ReadLine();
                Console.Write("Nova vrijednost: ");
                string val = Console.ReadLine();

                using (MemoryStream ms = new MemoryStream())
                {
                    formatter.Serialize(ms, a);
                    formatter.Serialize(ms, func);
                    formatter.Serialize(ms, val);
                    udpSocket.SendTo(ms.ToArray(), serverUdp);
                }

                rb = udpSocket.ReceiveFrom(buffer, ref ep);
                string poruka = Encoding.UTF8.GetString(buffer, 0, rb);
                Console.WriteLine(poruka);

                Console.Write("Još? (da/ne): ");
                string dalje = Console.ReadLine();
                udpSocket.SendTo(Encoding.UTF8.GetBytes(dalje), serverUdp);

                if (dalje == "ne")
                    break;

                // server šalje NOVU listu
                rb = udpSocket.ReceiveFrom(buffer, ref ep);
                using (MemoryStream ms = new MemoryStream(buffer, 0, rb))
                    uredjaji = (List<Appliance>)formatter.Deserialize(ms);
            }

            udpSocket.Close();
            clientSocket.Close();
        }
    }
}
