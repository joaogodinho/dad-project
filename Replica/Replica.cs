using System;
using System.Threading;
using CommonCode.Interfaces;
using CommonCode.Comms;
using CommonCode.Models;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Replica_project
{
    public class Replica : MarshalByRefObject, IReplica
    {
        private enum EStatus
        { 
            STOPPED,
            RUNNING,
            FROZEN
        }

        private string LogLevel { get; set; }
        private EStatus CurrStatus { get; set; } = EStatus.STOPPED;
        private int IInterval { get; set; } = 0;
        // private ConcurrentQueue<string[]> InBuffer { get; set; } = new ConcurrentQueue<string[]>();
        private ConcurrentQueue<string[]> OutBuffer { get; set; } = new ConcurrentQueue<string[]>();

        //Add fields as needed
        public Random myrandom { get; set; }
        public string MyId { get; set; }
        public Uri MyUri { get; set; }
        public string CurrentSemantic { get; set; }
        public static Operator MyOperator { get; set; }
        // This should just be a tuple? One for input, another for output and make a thread just to push the tuples out
        public BlockingCollection<DTO> InBuffer { get; set; }
        public IProcessCreationService PCS { get; set; }
        public Tuple<string,int> OPAndRep { get; set; }
        

        public Replica(string id, string myurl, Tuple<string,int> op_rep, string loglevel)
        {
            myrandom = new Random();
            MyId = id;
            MyUri = new Uri(myurl);
            InBuffer = new BlockingCollection<DTO>();
            OPAndRep = op_rep;
            PCS = (IProcessCreationService)Activator.GetObject(typeof(IProcessCreationService), myurl);
            MyOperator = PCS.getOperator(OPAndRep);
            LogLevel = loglevel;
            Task.Run(() =>
            {
                if (MyOperator.Input.StartsWith("OP"))
                    ProcessTuples();
                else ReadFile();
            });
        }

        // The loop that processes tuples from the input and puts them in the output
        // This should probably be its own thread, in order to handle the freeze/crash/unfreeze remote invocations
        private void ProcessTuples()
        {
            while (true) {
                lock (MyOperator)
                {
                    while (CurrStatus != EStatus.RUNNING)
                        Monitor.Wait(MyOperator);
                }
                DTO tuple = null;
                tuple = InBuffer.Take();
                mainProcessingCycle(tuple);

                if (IInterval != 0)
                {
                    Thread.Sleep(IInterval);
                }
            }
        }

        // The loop that send the tuples to the downstream operators
        // Like above, this should be its own thread, but shouldn't depend on the invocations
        private void SendTuples()
        {
            // While output queue is empty, block
                // Take the item and send it to the downstream replicas
                if (LogLevel == "full")
                {
                    // Notify either PCS or Puppet of the emitted tuple, now choose one...
                }
        }

        public bool processRequest(DTO blob)
        {
            Console.WriteLine("Received a tuple from " + blob.Sender);
            InBuffer.Add(blob);
            return true;
        }

        // TODO Make all of this async?
        public void ReadFile()
        {

            Stream stream = null;
            if ((stream = File.Open(AppDomain.CurrentDomain.BaseDirectory + MyOperator.Input, FileMode.Open)) != null)
            {
                using (stream)
                {
                    string test = (new StreamReader(stream)).ReadToEnd();
                    foreach (string s in test.Split('\n'))
                    {
                        if (!s.StartsWith("%") && !string.IsNullOrEmpty(s))
                        {
                            List<string> tuple = s.Split(',').Select(p => p.Trim()).ToList<string>();
                            
                            DTO dto = new DTO();
                            dto.Sender = MyUri.ToString();
                            dto.Tuple = tuple;
                            // Might not be the best solution for this, but for now...
                            if (MyOperator.DownIps.Count() > 0)
                            {
                                dto.Receiver = MyOperator.DownIps[0].ToString();
                            } 
                            else
                            {
                                dto.Receiver = null;
                            }
                            // Simulate upstream OP enqueuing tuples
                            InBuffer.Add(dto);
                        }
                    }
                    // Start normally
                    ProcessTuples();
                }
            }
            
        }

        private void ConsoleLog(string msg)
        {
            DateTime time = DateTime.Now;
            Console.WriteLine(time.ToString("[HH:mm:ss.fff]: ") + msg);
        }
        
        private void mainProcessingCycle(object blob)
        {
            DTO dto = (DTO)blob;
            ConsoleLog("Now processing tuple from " + dto.Sender);
            ConsoleLog("Tuple = " + String.Join(",", dto.Tuple.ToArray()));
            List<List<string>> result = MyOperator.Spec.processTuple(dto.Tuple);
            ConsoleLog("Finished processing tuple");


            if (result.Count == 0) { return; }
            else
            {
                foreach (List<string> tuple in result)
                {
                    ConsoleLog("Result = " + String.Join(",", tuple.ToArray()));
                    
                    if (MyOperator.DownIps.Count > 0)
                    {
                        IReplica replica = getDownstreamReplica(tuple);
                        DTO request = new DTO()
                        {
                            Sender = dto.Sender,
                            Tuple = tuple,
                            Receiver = MyOperator.DownIps[0].ToString() //TODO REMOVE THIS, IT IS VERY UNNECESSARY, MY FRIEND, THIS PIECE OF CODE DOES NOTHING, I WANT YOU TO REMOVE IT, MUCH THANKS.
                        };
                        ConsoleLog("Sending the tuple downstream to " + request.Receiver);
                        var requestResult = replica.processRequest(request);
                        ConsoleLog("Response from downstream was: " + requestResult);
                    }
                }
            }
        }

        public string PingRequest()
        {
            return "hey, you reached " + this.MyId + " on uri " + this.MyUri.ToString();
        }

        public void Start()
        {
            ConsoleLog("Got Start command");
            CurrStatus = EStatus.RUNNING;
            lock (MyOperator)
            {
                Monitor.Pulse(MyOperator);
            }
        }

        public void Interval(int time)
        {
            ConsoleLog("Got Interval command");
            Debug.Assert(time >= 0);
            IInterval = time;
        }
        

        public void Crash()
        {
            ConsoleLog("Got Crash command");
            Task.Run(() => Process.GetCurrentProcess().Kill());
        }

        public void Freeze()
        {
            ConsoleLog("Got Freeze command");
            CurrStatus = EStatus.FROZEN;
        }

        public void Unfreeze()
        {
            ConsoleLog("Got Unfreeze command");
            CurrStatus = EStatus.RUNNING;
            lock (MyOperator)
            {
                Monitor.Pulse(MyOperator);
            }
        }

        public void Status()
        {
            ConsoleLog("Got Status command");
            string result = "Status = ";
            switch (CurrStatus)
            {
                case EStatus.STOPPED:
                    result += "STOPPED";
                    break;
                case EStatus.RUNNING:
                    result += "RUNNING";
                    break;
                case EStatus.FROZEN:
                    result += "FROZEN";
                    break;
                default:
                    result += "UNKNOWN";
                    break;
            }
            ConsoleLog(result);
        }


        private IReplica getDownstreamReplica(List<string> tuple = null)
        {
            switch (MyOperator.Routing.Item1)
            {
                case "primary":
                    return (IReplica) Activator.GetObject(typeof(IReplica), MyOperator.DownIps[0].ToString());
                case "random":
                    return (IReplica)Activator.GetObject(typeof(IReplica), MyOperator.DownIps[myrandom.Next(MyOperator.DownIps.Count)].ToString());
                case "hashing":
                    int field = int.Parse(MyOperator.Routing.Item2);
                    int targetReplica = GetHashValue(tuple[field], MyOperator.DownIps.Count);
                    return (IReplica)Activator.GetObject(typeof(IReplica), MyOperator.DownIps[targetReplica].ToString());
                default:
                    throw new Exception("you dun goofed, there's no routing defined for this operator");
            }
        }

        private static int GetHashValue(string str, int value)
        {
            int counter = 0;
            foreach (char character in str.ToCharArray())
            {
                counter = +character;
            }
            return counter % value;
        }

    }
}
