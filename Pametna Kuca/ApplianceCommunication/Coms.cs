using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using HomeAppliances;

namespace ApplianceCommunication
{
    public class Coms
    {
        static void Main(string[] args)
        {
            int number = 0;
            foreach (var arg in args)
            {
                Console.WriteLine("Shipment delivered.");
                number = Int32.Parse(arg);
            }
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint destinationEP = new IPEndPoint(IPAddress.Any, number);
            udpSocket.Bind(destinationEP);

            EndPoint senderEP = new IPEndPoint(IPAddress.Any, 0);   
            Appliance appliance = new Appliance();
            List<Appliance> appliances = appliance.ListOfAppliances();
            bool flag = false;

            while (!flag)
            { 
                byte[] buffer = new byte[1024];
                try 
                { 
                    int bytesReceived = udpSocket.ReceiveFrom(buffer, ref senderEP);
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                    Console.WriteLine("Message recieved "+ receivedMessage);
                    if (receivedMessage == "Server is done with work.") 
                    {
                        flag = true;
                        break;
                    }
                    Console.WriteLine($"Message recieved from {senderEP}, length {bytesReceived}->:{receivedMessage}");

                    string[] parts = receivedMessage.Split(':');
                    Console.WriteLine(parts.Length+" " + parts[0]+ " "+ parts[1] + " " + parts[2]);
                    foreach (var app in appliances)
                    {
                        if (app.Name == parts[0])
                        {
                            app.AddCommand(parts[1], parts[2]);
                            Console.WriteLine("Command added to appliance: " + app.Name);
                            break;
                        }
                    }

                    foreach (var app in appliances)
                    {
                        Console.WriteLine("Appliance: " + app.Name);
                        Console.WriteLine("Commands:");
                        foreach (var command in app.CommandList)
                        {
                            Console.WriteLine("- " + command);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error receiving message: " + ex.Message);
                }
            }
            Console.WriteLine("End of appliance.");
            udpSocket.Close();
            Console.ReadKey();
        }
    }
}
