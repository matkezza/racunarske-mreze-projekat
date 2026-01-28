using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

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
                    odgovor = Encoding.UTF8.GetString(buffer, 0, brojBajta);

                    Console.WriteLine("Log in: " + odgovor);
                    clientSocket.Blocking = true;
                }
            }
            while (odgovor != "USPESNO");

            brojBajta = clientSocket.Receive(buffer);

            if(brojBajta > 0)
            {
                odgovor = Encoding.UTF8.GetString(buffer, 0, brojBajta);
                Console.WriteLine("UDP Port->" + odgovor);
            }

            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            EndPoint deviceEndPoint = new IPEndPoint(IPAddress.Any, 0);
            int assignedPort;
            IPEndPoint destinationEP = new IPEndPoint(IPAddress.Loopback, 0);

            if(int.TryParse(odgovor, out assignedPort))
            {
                destinationEP = new IPEndPoint(IPAddress.Loopback, assignedPort);
            }

            string initialMessage = $"Klijent se povezao na UDP port: {assignedPort}";
            byte[] initialData = Encoding.UTF8.GetBytes(initialMessage);

            udpSocket.SendTo(initialData, destinationEP);
            Console.WriteLine($"Poruka poslata serveru: {initialMessage}");



            try
            {
                while (true)
                {
                    brojBajta = udpSocket.ReceiveFrom(buffer, ref deviceEndPoint);
                    if (brojBajta > 0)
                    {

                        List<Uredjaj> uredjaji = new List<Uredjaj>();
                        using (MemoryStream ms = new MemoryStream(buffer, 0, brojBajta))
                        {
                            uredjaji = (List<Uredjaj>)formatter.Deserialize(ms);
                        }

                        Console.WriteLine("Lista dostupnih uređaja:");
                        for (int i = 0; i < uredjaji.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {uredjaji[i].Ime} (Port: {uredjaji[i].Port})");
                        }

                        int izbor;
                        do
                        {
                            Console.WriteLine("Unesite redni broj uređaja koji želite da podesite:");
                            izbor = Int32.Parse(Console.ReadLine()) - 1;
                        } while (izbor < 0 || izbor >= uredjaji.Count);

                        Uredjaj izabraniUredjaj = uredjaji[izbor];
                        Console.WriteLine($"Izabrali ste uređaj: {izabraniUredjaj.Ime}");
                        Console.WriteLine("[Ime funkcije, Vrednost]");
                        Console.WriteLine("--------------------------");
                        foreach (var v in izabraniUredjaj.Funkcije)
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
                            foreach (var f in izabraniUredjaj.Funkcije)
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

                        initialData = Encoding.UTF8.GetBytes($"{izabraniUredjaj.Ime}:{funkcija}:{vrednost}");
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
}
