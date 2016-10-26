using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace dad_project
{
    class PuppetMaster
    {
        private List<Operator> operators = new List<Operator>();

        static void Main(string[] args)
        {
            // Make sure configuration file is sent as an argument
            if (args.Length != 1)
            {
                Console.WriteLine("Configuration file missing!");
                return;
            }
            new PuppetMaster(args[0]);
            Console.ReadLine();
        }

        public PuppetMaster(string filename)
        {
            Console.WriteLine("Initializing Pupper Master...");
            string[] conf = System.IO.File.ReadAllLines(@filename);
            parseConfig(conf);
        }

        private void parseConfig(string[] conf)
        {
            string heading = @"^OP(?<id>\d+)\s+INPUT_OPS\s+(?<input>\S+)$";
            string replicas = @"REP_FACT\s+(?<nrep>\d)\s+ROUTING\s(?<routing>hashing\((?<hash_field>\d+)\)|random|primary)$";
            string addresses = @"ADDRESS\s+(.*)$";
            string operator_spec = @"OPERATOR_SPEC\s+(?<op>UNIQ|COUNT|DUP|FILTER)";
            string uniq = @"UNIQ\s+(?<uniq>\d+)$";
            string filter = @"FILTER\s(?<field>\d+),\s*\""(?<cond>[^\""]+)\"",\s*\""(?<value>[^\""]*)\""$";
            string custom = @"CUSTOM\s\""(?<dll>[^\""]+)\"",\s*\""(?<class>[^\""]+)\"",\s*\""(?<method>[^\""]*)\""$";

            Regex rxHeading = new Regex(heading, RegexOptions.Compiled);
            Regex rxReplicas= new Regex(replicas, RegexOptions.Compiled);
            Regex rxAddresses= new Regex(addresses, RegexOptions.Compiled);
            // Regex rxAddresses= new Regex(addresses, RegexOptions.Compiled);

            Console.WriteLine("Parsing Operators...");
            for (int i = 0; i < conf.Length; i++)
            {
                if (conf[i] == "") { continue; }
                // First thing should be the OP heading
                Match match = rxHeading.Match(conf[i]);
                Console.WriteLine(match.Groups["id"]);
                Console.WriteLine(match.Groups["input"]);

                Console.WriteLine();
          
                MatchCollection matches = rxHeading.Matches(conf[i]);
                Console.WriteLine("{0} Matches found", matches.Count);
            }
        }
    }
}
