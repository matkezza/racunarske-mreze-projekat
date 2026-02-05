using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserLibrary
{
    [Serializable]
    public class ClientL
    {
        public string Name { get; set; }
        
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Status { get; set; }
        public int Port { get; set; }
        
        public ClientL(string name, string lastName, string username, string password, bool status,int port)
        {
            Name = name;
            LastName = lastName;
            Username = username;
            Password = password;
            Status = status;
            Port = port;
        }

        public ClientL()
        {
        }

        public ClientL FindClient(List<ClientL> clients, string username, string password)
        {
            foreach (var client in clients)
            {
                if(client.Username == username && client.Password == password)
                {
                    client.Status = true;
                    return client;
                }
            }
            return null;
        }

        public void FindPort(List<ClientL> clients, int port) 
        {
            foreach(var client in clients )
            {
                if(client.Port == port)
                {
                    client.Status = true;
                }
            }
        }

        public bool FindInactiveClient(List<ClientL> clients, string username)
        {
            foreach (var client in clients)
            {
                if (client.Username == username && client.Status == false)
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"Name: {Name}, LastName: {LastName}, Username: {Username}, Password: {Password}, Status: {Status}, Port: {Port}";
        }

        public void PrintClientInfo(List<ClientL> clients)
        {
            Console.WriteLine("--------------------------------------------------------------------------");
            Console.WriteLine("| NAME     | LASTNAME       | USERNAME    | LOGGED IN    |PASSWORD       |");
            Console.WriteLine("--------------------------------------------------------------------------");

            foreach(var client in clients)
            {
                Console.WriteLine($"| {client.Name,-8} | {client.LastName,-14} | {client.Username,-11} | {(client.Status ? "Yes" : "No"),-12} | {client.Password,-14} |");
            }
                Console.WriteLine("--------------------------------------------------------------------------");

        }
    }
}
