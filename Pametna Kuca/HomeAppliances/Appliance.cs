using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeAppliances
{
    [Serializable]
    public class Appliance
    {
        public string Name { get; set; }
        public int Port { get; set; }
        public Dictionary<string,string> Functions { get; set; }
        public List<string> CommandList { get; set; }
        public DateTime LastChange { get; set; }

        public List<Appliance> appliances { get; set; }
        
        public Appliance(string name, int port, Dictionary<string,string> functions, List<string> commandList, DateTime lastChange)
        {
            Name = name;
            Port = port;
            Functions = functions;
            CommandList = new List<string>();
            LastChange = DateTime.Now;
        }

        public Appliance(string name, int port, Dictionary<string, string> functions)
        {
            Name = name;
            Port = port;
            Functions = functions;
            CommandList = new List<string>();
            LastChange = DateTime.Now;
        }

        public Appliance()
        {
            appliances = new List<Appliance> {
            new Appliance("Air Conditioner", 2000, new Dictionary<string, string> { {"current state","off"},{"temperature","22"}}),
            new Appliance("LED light", 5555, new Dictionary<string, string> { {"WATS","15"},{"color","yellow"},{"blue light","50"} })
            };
        }
    }
}
