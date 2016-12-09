using CommonCode.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonCode.Comms;
using System.Diagnostics;
using static CommonCode.Comms.DTO;
using System.IO;
using CommonCode.Models;

namespace ProcessCreationService_project
{
    public class ProcessCreationService : MarshalByRefObject, IProcessCreationService
    {
        public override object InitializeLifetimeService()
        {

            return null;

        }

        public const int PORT = 10001;
        public const string NAME = "pcs";

        private Uri MyUri { get; set; }
        private string MyName { get; set; }

        private Dictionary<string, List<Operator>> Operators;
        private string LoggingLevel { get; set; }
        private string Semantics { get; set; }

        public static string BuildURI(string ip)
        {
            if (!ip.StartsWith("tcp://"))
            {
                ip = "tcp://" + ip;
            }
            return ip + ":" + PORT + "/" + NAME;
        }

        public ProcessCreationService()
        {
            Operators = new Dictionary<string, List<Operator>>();
        }

        public string PingRequest()
        {
            return "PONG";
        }

        public void Config(string loglevel, string semantics)
        {
            LoggingLevel = loglevel;
            Semantics = semantics;
            ConsoleLog("Got LoggingLevel: " + LoggingLevel);
            ConsoleLog("Got Semantics: " + Semantics);
        }

        public void AddOperator(Operator op)
        {
            ConsoleLog("Adding new operator with ID: " + op.Id);
            List<Operator> tempOperators = null;
            Operators.TryGetValue(op.Id.Item1,out tempOperators);
            if (tempOperators == null)
            {
                tempOperators = new List<Operator>();
                Operators.Add(op.Id.Item1, tempOperators); 
            }
            op.Replica = (IReplica)Activator.GetObject(typeof(IReplica), "tcp://localhost:" + op.Port + "/op");
            tempOperators.Add(op);
            Process.Start(AppDomain.CurrentDomain.BaseDirectory + "Replica.exe",
                String.Format("{0} {1} {2} {3} {4} {5} {6}", op.Id.Item1, op.Id.Item2, op.Port, op.PCS, LoggingLevel, Semantics, "tcp://localhost:10000/puppetmaster"));

        }

        private void ConsoleLog(string msg)
        {
            DateTime time = DateTime.Now;
            Console.WriteLine(time.ToString("[HH:mm:ss.fff]: ") + msg);
        }

        public Operator getOperator(Tuple<string, int> op_rep)
        {
            return Operators[op_rep.Item1].Where(x => x.Id.Item1 == op_rep.Item1 && x.Id.Item2 == op_rep.Item2).FirstOrDefault();
        }

        private delegate void RemoteFuncCall(IReplica rep);

        // For commands that take an OP
        private void CallOnRep(string op, RemoteFuncCall func)
        {
            Operators[op].ForEach(x => func(x.Replica));
        }

        // For global commands, no OP
        private void CallOnRep(RemoteFuncCall func)
        {
            Operators.Values.ToList().ForEach(x => x.ForEach(y => func(y.Replica)));
        }

        // For commands that take OP and ID
        private void CallOnRep(Tuple<string, int> id, RemoteFuncCall func)
        {
            List<Operator> operators = Operators[id.Item1];
            foreach (Operator op in operators)
            {
                if (op.Id.Item2 == id.Item2)
                {
                    func(op.Replica);
                    break;
                }
            }
        }

        public void Start(string op)
        {
            ConsoleLog("Starting " + op);
            CallOnRep(op, x => x.Start());
        }

        public void Interval(string op, int time)
        {
            ConsoleLog("Setting inverval on " + op + " as " + time);
            CallOnRep(op, x => x.Interval(time));
        }

        public void Status()
        {
            CallOnRep(x => x.Status());
        }

        public void Crash(Tuple<string, int> id)
        {
            ConsoleLog("Crashing " + id.Item1 + " " + id.Item2);
            CallOnRep(id, x => x.Crash());
            // Remove the crashed replica from the PCS
            List<Operator> operators = Operators[id.Item1];
            operators.Remove(operators.FirstOrDefault(op => op.Id.Item2 == id.Item2));
        }

        public void Freeze(Tuple<string, int> id)
        {
            ConsoleLog("Freezing " + id.Item1 + " " + id.Item2);
            CallOnRep(id, x => x.Freeze());
        }

        public void Unfreeze(Tuple<string, int> id)
        {
            ConsoleLog("Unfreezing " + id.Item1 + " " + id.Item2);
            CallOnRep(id, x => x.Unfreeze());
        }

        // If no command was executed, this crashes due to forced connection closed
        public void Reset()
        {
            // Crash all replicas
            Operators.Values.ToList().ForEach(x => x.ForEach(y => y.Replica.Crash()));
            // Reset ops
            Operators = new Dictionary<string, List<Operator>>();
        }
    }
}
