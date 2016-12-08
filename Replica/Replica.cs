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
using System.Collections.Generic;
using System.Net.Sockets;

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

        // Time before resending a tuple
        private const int PROCESS_TIMEOUT = 5000;

        private string LogLevel { get; set; }
        private EStatus CurrStatus { get; set; } = EStatus.STOPPED;
        private int IInterval { get; set; } = 0;
        private IPuppet PuppetMaster { get; set; }
        private Semantic MySemantic { get; set; }
        private ConcurrentDictionary<string, byte> ProcessedTuples { get; set; }
        private delegate int GetDownstreamDel(List<string> tuple);
        private GetDownstreamDel GetDownStream { get; set; }
        private int tuplecounter;
        public int TupleCounter
        {
            get
            {
                return tuplecounter++;
            }
            set { tuplecounter = value; }
        }
        private BlockingCollection<string> ConsoleBuffer { get; set; } = new BlockingCollection<string>();

        public Random MyRandom { get; set; }
        public Uri MyUri { get; set; }
        public static Operator MyOperator { get; set; }
        public BlockingCollection<DTO> InBuffer { get; set; }
        public IProcessCreationService PCS { get; set; }
        public Tuple<string,int> OPAndRep { get; set; }
        
        // TODO Deal with unused first arg
        public Replica(string id, string myurl, Tuple<string,int> op_rep, string loglevel, string pmurl)
        {
            TupleCounter = 0;
            // TODO Receive this as an argument
            // MySemantic = AtMostOnce;
            MySemantic = AtLeastOnce;
            MyRandom = new Random();
            MyUri = new Uri(myurl);
            InBuffer = new BlockingCollection<DTO>();
            OPAndRep = op_rep;
            PCS = (IProcessCreationService)Activator.GetObject(typeof(IProcessCreationService), myurl);
            PuppetMaster = (IPuppet)Activator.GetObject(typeof(IReplica), pmurl);
            MyOperator = PCS.getOperator(OPAndRep);

            switch (MyOperator.Routing.Item1)
            {
                case "primary":
                    GetDownStream = (x) => { return 0; };
                    break;
                case "random":
                    GetDownStream = (x) => { return MyRandom.Next(MyOperator.DownIps.Count()); };
                    break;
                case "hashing":
                    int field = int.Parse(MyOperator.Routing.Item2) - 1;
                    GetDownStream = (x) => { return GetHashValue(x[field], MyOperator.DownIps.Count); };
                    break;
                default:
                    ConsoleLog("Got invalid routing.");
                    throw new Exception("Invalid Routing");
            }

            LogLevel = loglevel;
            ProcessedTuples = new ConcurrentDictionary<string, byte>();
            Task.Run(() =>
            {
                if (MyOperator.Input.StartsWith("OP"))
                    ProcessTuples();
                else ReadFile();
            });

            Task.Run(() =>
            {
                while (true)
                {
                    Console.WriteLine(ConsoleBuffer.Take());
                }
            }
            );
        }

        // The loop that processes tuples from the input and puts them in the output
        // This should probably be its own thread, in order to handle the freeze/crash/unfreeze remote invocations
        private void ProcessTuples()
        {
            while (true) {
                DTO tuple = InBuffer.Take();
                lock (MyOperator)
                {
                    while (CurrStatus != EStatus.RUNNING)
                        Monitor.Wait(MyOperator);
                }
                mainProcessingCycle(tuple);
                if (IInterval != 0)
                {
                    Thread.Sleep(IInterval);
                }
            }
        }

        public bool processRequest(DTO blob)
        {
            ConsoleLog("Received a tuple from " + blob.Sender);
            InBuffer.Add(blob);
            return true;
        }

        public void ReadFile()
        {

            Stream stream = null;
            string fileContent = "";
            if ((stream = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + MyOperator.Input)) != null)
            {
                using (stream)
                {
                    fileContent = (new StreamReader(stream)).ReadToEnd();
                }
            } else
            {
                throw new Exception("Could not open file.");
            }
            foreach (string s in fileContent.Split('\n'))
            {
                if (!s.StartsWith("%") && !string.IsNullOrEmpty(s))
                {
                    List<string> tuple = s.Split(',').Select(p => p.Trim()).ToList<string>();
                            
                    DTO dto = new DTO();
                    dto.Sender = MyUri.ToString();
                    dto.Tuple = tuple;
                    // No point in having an ID when reading from file
                    dto.ID = "";
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

        private void ConsoleLog(string msg)
        {
            DateTime time = DateTime.Now;
            ConsoleBuffer.Add(time.ToString("[HH:mm:ss.fff]: ") + msg);
        }
        
        private void mainProcessingCycle(object blob)
        {
            DTO dto = (DTO)blob;
            ConsoleLog("Processing " + String.Join(",", dto.Tuple.ToArray()));
            List<List<string>> result = MyOperator.Spec.processTuple(dto.Tuple);
            // Add to the processed queue
            ProcessedTuples.TryAdd(dto.ID, 0);

            if (result.Count == 0) { return; }
            else
            {
                foreach (List<string> tuple in result)
                {
                    string msg = "Emitting " + String.Join(",", tuple);
                    ConsoleLog(msg);
                    if (LogLevel == "full")
                    {
                        PuppetMaster.SendMsg(MyOperator.Id.Item1 + "#" + MyOperator.Id.Item2 + ": " + msg);
                    }
                    
                    if (MyOperator.DownIps.Count > 0)
                    {
                        // Send asynchronously
                        Task.Run(() => SendTuple(tuple));
                    }
                }
            }
        }

        private void SendTuple(List<string> tuple)
        {
            ConsoleLog("Sending tuple downstream...");
            while (MyOperator.DownIps.Count > 0)
            {
                int index = GetDownStream(tuple);
                IReplica downRep = (IReplica)Activator.GetObject(typeof(IReplica), MyOperator.DownIps[index].ToString());
                DTO req = new DTO()
                {
                    Sender = MyOperator.Id.ToString(),
                    Tuple = tuple,
                    Receiver = null,
                    ID = OPAndRep.ToString() + TupleCounter
                };
                try
                {
                    MySemantic(downRep, req);
                    return;
                } catch (SocketException e)
                {
                    ConsoleLog("Downstream is down, rerouting...");
                    MyOperator.DownIps.RemoveAt(index);
                }
            }
            ConsoleLog("No downstream available.");
        }

        delegate void Semantic(IReplica replica, DTO req);

        // Keep firing until there is a confirmation is was processed
        private void AtLeastOnce(IReplica replica, DTO req)
        {
            bool answer_received = false;
            while (!answer_received)
            {
                // Send the tuple
                replica.processRequest(req);
                Thread.Sleep(PROCESS_TIMEOUT);
                ConsoleLog("Checking if" + req.ID + " was processed...");
                if (replica.TupleProcessed(req))
                {
                    ConsoleLog(req.ID +" Processed.");
                    answer_received = true;
                }
            }
        }

        // Fire one time, whatever happens, happens
        private void AtMostOnce(IReplica replica, DTO req)
        {
            replica.processRequest(req);
        }

        public string PingRequest()
        {
            return "PONG";
        }

        public void Start()
        {
            ConsoleLog("Starting...");
            CurrStatus = EStatus.RUNNING;
            lock (MyOperator)
            {
                Monitor.Pulse(MyOperator);
            }
        }

        public void Interval(int time)
        {
            Debug.Assert(time >= 0);
            ConsoleLog("Setting interval to " + time);
            IInterval = time;
        }
        

        public void Crash()
        {
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


        private int getDownstreamReplica(List<string> tuple = null)
        {
            switch (MyOperator.Routing.Item1)
            {
                case "primary":
                    return 0;
                case "random":
                    return MyRandom.Next(MyOperator.DownIps.Count());
                case "hashing":
                    int field = int.Parse(MyOperator.Routing.Item2) - 1;
                    return GetHashValue(tuple[field], MyOperator.DownIps.Count);
                default:
                    throw new Exception("you dun goofed, there's no routing defined for this operator");
            }
        }

        private static int GetHashValue(string str, int value)
        {
            int counter = 0;
            foreach (char character in str.ToCharArray())
            {
                counter += character;
            }
            return counter % value;
        }

        public bool TupleProcessed(DTO req)
        {
            ConsoleLog("Got tuple processed question for " + req.ID);
            byte _;
            if (ProcessedTuples.TryRemove(req.ID, out _))
            {
                return true;
            }
            return false;
        }
    }
}
