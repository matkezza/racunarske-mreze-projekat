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

        public void AddCommand(string command,string value)
        {
            if (Functions.ContainsKey(command))
            {
                Functions[command] = value;
            }
            else 
            {
                Functions.Add(command, value);
            }

            LastChange = DateTime.Now;
            CommandList.Add($"{command}:{value}");
        }

        public string GetState()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Appliance: {Name}");
            sb.AppendLine($"Port: {Port}");
            sb.AppendLine("Functions:");
            foreach (var function in Functions)
            {
                sb.AppendLine($"- {function.Key}: {function.Value}");
            }
            sb.AppendLine($"Last Change: {LastChange}");
            return sb.ToString();
        }

        public List<Appliance> ListOfAppliances()
        {
            return appliances;
        }

        public void UpdateList(List<Appliance> newAppliance) { 
            appliances= newAppliance;
        }

        public string GetAllAppliances(List<Appliance> appliance)
        { 
            string tabel = string.Format("{0,-20} {1,-10} {2,-30} {3,-20}", "Name", "Port", "Functions", "Last Change");
            tabel += "\n" + new string('-', 80) + "\n";

            foreach (var app in appliance) { 
                string functions = string.Join(", ", app.Functions.Select(f => $"{f.Key}:{f.Value}"));
                tabel += string.Format("{0,-20} {1,-10} {2,-30} {3,-20}", app.Name, app.Port, functions, app.LastChange);
            }
            return tabel;
        }

        public string GetAllCommands()
        {
            string tabel = string.Format("{0,-20} {1,-30}", "Appliance Name", "Commands");
            tabel += "\n" + new string('-', 50) + "\n";

            string functions =string.Join(", ", Functions.Select(f => $"{f.Key}:{f.Value}"));
            tabel += string.Format("{0,-20} {1,-30}", Name, functions);

            return tabel;

        }
    }
}
