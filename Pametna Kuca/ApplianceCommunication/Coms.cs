using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using HomeAppliances;

namespace ApplianceCommunication
{
    public class Coms
    {
        static void Main(string[] args)
        {
            int listenPort = 0;
            foreach (var arg in args)
            {
                listenPort = Int32.Parse(arg);
            }

            if (listenPort == 0)
            {
                Console.WriteLine("Pokretanje: ApplianceCommunication.exe <PORT>");
                return;
            }

            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));

            IPEndPoint destinationEP = new IPEndPoint(IPAddress.Any, listenPort);
            udpSocket.Bind(destinationEP);

            EndPoint senderEP = new IPEndPoint(IPAddress.Any, 0);

            Appliance applianceSeed = new Appliance();
            List<Appliance> appliances = applianceSeed.ListOfAppliances();

            Console.WriteLine($"[DEVICE] Listening on UDP port {listenPort}...");

            bool stop = false;
            while (!stop)
            {
                byte[] buffer = new byte[4096];
                try
                {
                    int bytesReceived = udpSocket.ReceiveFrom(buffer, ref senderEP);
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesReceived).Trim();

                    if (receivedMessage == "Server is done with work.")
                    {
                        stop = true;
                        break;
                    }

                    Console.WriteLine($"[DEVICE:{listenPort}] From {senderEP} -> {receivedMessage}");

                    // Expect: ApplianceName:Function:Value
                    string[] parts = receivedMessage.Split(':');
                    if (parts.Length < 3)
                    {
                        SendFeedback(udpSocket, senderEP, $"Neispravna komanda: '{receivedMessage}'");
                        continue;
                    }

                    string applianceName = parts[0];
                    string func = parts[1];
                    string val = parts[2];

                    Appliance target = null;
                    foreach (var app in appliances)
                    {
                        if (app.Name == applianceName)
                        {
                            target = app;
                            break;
                        }
                    }

                    if (target == null)
                    {
                        SendFeedback(udpSocket, senderEP, $"Uređaj '{applianceName}' nije pronađen na portu {listenPort}.");
                        continue;
                    }

                    target.AddCommand(func, val);

                    // Pretty feedback to server
                    string feedback = $"{target.Name}: {func} postavljeno na '{val}'.";
                    SendFeedback(udpSocket, senderEP, feedback);

                    // Print local state
                    Console.WriteLine(target.GetState());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error receiving message: " + ex.Message);
                }
            }

            Console.WriteLine("End of appliance.");
            udpSocket.Close();
        }

        private static void SendFeedback(Socket sock, EndPoint remote, string msg)
        {
            try
            {
                byte[] fb = Encoding.UTF8.GetBytes(msg);
                sock.SendTo(fb, remote);
            }
            catch { }
        }
    }
}
