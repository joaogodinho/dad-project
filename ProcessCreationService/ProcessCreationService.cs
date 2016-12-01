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
        public const int PORT = 10001;
        public const string NAME = "pcs";

        private Uri MyUri { get; set; }
        private string MyName { get; set; }
        private IPuppet PuppetMaster { get; set; }

        // TODO This should be inside Operators
        private Dictionary<string, List<IReplica>> Replicas;
        private Dictionary<string, List<Operator>> Operators;
        // TODO Need to pass this down to the Replicas
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
            Replicas = new Dictionary<string, List<IReplica>>();
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
            Console.WriteLine("Got LoggingLevel: " + LoggingLevel);
            Console.WriteLine("Got Semantics: " + Semantics);
        }

        public void AddOperator(Operator op)
        {
            Console.WriteLine("Adding new operator with ID: " + op.Id);
            List<Operator> tempOperators = null;
            Operators.TryGetValue(op.Id.Item1,out tempOperators);
            if (tempOperators == null)
            {
                tempOperators = new List<Operator>();
                Operators.Add(op.Id.Item1, tempOperators); 
            }
            op.Replica = (IReplica)Activator.GetObject(typeof(IReplica), "tcp://localhost:" + op.Port + "/op");
            tempOperators.Add(op);
            Process.Start(AppDomain.CurrentDomain.BaseDirectory + "Replica.exe", op.Id.Item1 + " " + op.Id.Item2 + " " + op.Port + " " + op.PCS + " " + LoggingLevel);

        }


        public Operator getOperator(Tuple<string, int> op_rep)
        {
            return Operators[op_rep.Item1].Where(x => x.Id.Item1 == op_rep.Item1 && x.Id.Item2 == op_rep.Item2).FirstOrDefault();
        }

        private delegate void RemoteFuncCall(IReplica rep);
        // Maybe this is what they mean when they say the commands should be async,
        // the calls to the replicas should prob be async
        // TODO implement the call as a new thread



        private void CallOnRep(Tuple<string, int> id, RemoteFuncCall func)
        {
            if (id.Item1 == "ALL")
            {
                Operators.Values.ToList().ForEach(x => x.ForEach(y => func(y.Replica)));
            }
            else
            {
                List<Operator> operators = Operators[id.Item1];
                foreach (Operator op in operators)
                {
                    if (id.Item2 != -1 && op.Id.Item2 == id.Item2)
                    {
                        func(op.Replica);
                    }
                    else
                    {
                        func(op.Replica);
                    }
                }
            }
        }

        public void Start(string op)
        {
            Operators[op].ForEach((x) => {
                Console.WriteLine("Starting op @ " + x.Replica);
                x.Replica.Start();
            });
            //CallOnRep(new Tuple<string, int>(op, -1), x => x.Start());
        }

        public void Interval(string op, int time)
        {
            Operators[op].ForEach((x) => {
                Console.WriteLine("Setting interval for " + op + " as " + time + "ms");
                x.Replica.Interval(time);
            });
        }

        public void Status()
        {
            //CallOnRep(new Tuple<string, int>("ALL", -1), x => x.Status());
        }

        public void Crash(Tuple<string, int> id)
        {
            Operators[id.Item1].Where(x => x.Id.Item2 == id.Item2).ToList().ForEach((x) => {
                Console.WriteLine("Crashing op" + id.Item1 + "-" + id.Item2);
                x.Replica.Crash();
            });
        }

        public void Freeze(Tuple<string, int> id)
        {
            Operators[id.Item1].Where(x => x.Id.Item2 == id.Item2).ToList().ForEach((x) => {
                Console.WriteLine("Freezing op"+id.Item1 +"-" + id.Item2);
                x.Replica.Freeze();
            });
        }

        public void Unfreeze(Tuple<string, int> id)
        {
            Operators[id.Item1].Where(x => x.Id.Item2 == id.Item2).ToList().ForEach((x) => {
                Console.WriteLine("Unfreezing op" + id.Item1 + "-" + id.Item2);
                x.Replica.Unfreeze();
            });
        }




    }
}
