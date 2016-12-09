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

        // Used delegates
        private delegate void SemanticDel(IReplica replica, DTO req);
        private delegate int GetDownstreamDel(List<string> tuple);

        // Yeah bad seeding, not a gambling site, who cares
        private Random MyRandom { get; set; } = new Random();

        // General props
        private Tuple<string,int> OPAndRep { get; set; }
        private string LogLevel { get; set; }
        private int IInterval { get; set; } = 0;
        private static Operator MyOperator { get; set; }
        private EStatus CurrStatus { get; set; } = EStatus.STOPPED;
        private Uri MyUri { get; set; }
        private GetDownstreamDel GetDownStream { get; set; }
        private BlockingCollection<string> ConsoleBuffer { get; set; } = new BlockingCollection<string>();
        private BlockingCollection<DTO> InBuffer { get; set; } = new BlockingCollection<DTO>();
        private IProcessCreationService PCS { get; set; }
        private IPuppet PuppetMaster { get; set; }
        private Object DownIpsLock = new Object();

        // Semantics related
        private SemanticDel MySemantic { get; set; }
        private ConcurrentDictionary<string, byte> ProcessedTuplesID { get; set; } = new ConcurrentDictionary<string, byte>();
        private const int PROCESS_TIMEOUT = 2000;
        private Object TupleCounterLock = new Object();
        private int tuplecounter;
        private int TupleCounter
        {
            get
            {
                return tuplecounter++;
            }
            set { tuplecounter = value; }
        }


        
        public Replica(string myurl, Tuple<string,int> op_rep, string loglevel, string semantics, string pmurl)
        {
            OPAndRep = op_rep;
            LogLevel = loglevel;
            MyUri = new Uri(myurl);

            PCS = (IProcessCreationService)Activator.GetObject(typeof(IProcessCreationService), myurl);
            PuppetMaster = (IPuppet)Activator.GetObject(typeof(IReplica), pmurl);
            MyOperator = PCS.getOperator(OPAndRep);
            Console.WriteLine("My Replicas are @ " + String.Join(";",MyOperator.ReplicasUris));
            SetRouting(MyOperator.Routing);

            TupleCounter = 0;
            SetSemantics(semantics);

            Task.Run(() =>
            {
                if (MyOperator.Input.StartsWith("OP"))
                    ProcessTuples();
                else ReadFile();
            });

            // Use a thread for printing to the console, because being I/O bound sucks
            Task.Run(() =>
            {
                while (true)
                {
                    Console.WriteLine(ConsoleBuffer.Take());
                }
            }
            );
        }

        private void SetRouting(Tuple<string, string> routing)
        {
            switch (routing.Item1)
            {
                case "primary":
                    GetDownStream = (_) => { return 0; };
                    break;
                case "random":
                    GetDownStream = (_) => { return MyRandom.Next(MyOperator.DownIps.Count()); };
                    break;
                case "hashing":
                    int field = int.Parse(routing.Item2) - 1;
                    GetDownStream = (x) => { return GetHashValue(x[field], MyOperator.DownIps.Count); };
                    break;
                default:
                    ConsoleLog("Got invalid routing.");
                    throw new Exception("Invalid Routing");
            }
        }

        private void SetSemantics(string semantics)
        {
            switch (semantics)
            {
                case "at-most-once":
                    MySemantic = AtMostOnce;
                    break;
                case "at-least-once":
                    MySemantic = AtLeastOnce;
                    break;
                case "exactly-once":
                    throw new NotImplementedException("Exactly-once not implemented.");
                default:
                    ConsoleLog("Invalid semantic.");
                    throw new Exception("Invalid semantics");
            }
        }

        // Main processing cycle
        private void ProcessTuples()
        {
            while (true) {
                DTO dto = InBuffer.Take();
                lock (MyOperator)
                {
                    while (CurrStatus != EStatus.RUNNING)
                        Monitor.Wait(MyOperator);
                }

                byte _;
                // Exactly-once starts here
                if (ProcessedTuplesID.TryGetValue(dto.ID, out _)) { continue; }
                // set processing = X
                // foreach other replica
                    // check if being processed or already processed
                // Process the tuple
                List<List<string>> result = ProcessingOperation(dto);

                //verificar se não foi já processado, por mim e por outras replicas (notificar que vais processar este tuplo)
                //caso já tenha sido processado ou esteja a ser processado, continue;
                //no fim do processamento, avisar que já foi processado às outras replicas, adicionar aos processados (replicas primeiro, meus no final)

                // Throw this to someone else, my cycle is for processing only
                Task.Run(() =>
                {
                    foreach (List<string> tuple in result)
                    {
                        string msg = "Emitting " + String.Join(",", tuple);
                        ConsoleLog(msg);
                        if (LogLevel == "full")
                        {
                            PuppetMaster.SendMsg(MyOperator.Id.Item1 + "#" + MyOperator.Id.Item2 + ": " + msg);
                        }
                        // Send asynchronously
                        Task.Run(() => SendTuple(tuple));
                    }

                    if (IInterval != 0)
                    {
                        Thread.Sleep(IInterval);
                    }
                });
            }
        }

        public bool processRequest(DTO blob)
        {
            ConsoleLog("Received a tuple: " + blob.ID);
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
                    int id;
                    lock (TupleCounterLock)
                    {
                        id = TupleCounter;
                    }
                    dto.ID = OPAndRep.ToString() + id;
                    if (MyOperator.DownIps.Count() > 0)
                    {
                        dto.Receiver = MyOperator.DownIps[GetDownStream(tuple)].ToString();
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
        
        private List<List<string>> ProcessingOperation(DTO dto)
        {
            
            ConsoleLog("Processing " + String.Join(",", dto.Tuple.ToArray()));
            List<List<string>> result = MyOperator.Spec.processTuple(dto.Tuple);
            // Add to the processed id queue
            ProcessedTuplesID.TryAdd(dto.ID, 0);
            return result;
        }

        private void SendTuple(List<string> tuple)
        {
            ConsoleLog("Sending tuple downstream...");
            while (MyOperator.DownIps.Count > 0)
            {
                int index;
                int id;
                IReplica downRep;
                lock (DownIpsLock)
                {
                    index = GetDownStream(tuple);
                    downRep = (IReplica)Activator.GetObject(typeof(IReplica), MyOperator.DownIps[index].ToString());
                }
                lock (TupleCounterLock)
                {
                    id = TupleCounter;
                }
                DTO req = new DTO()
                {
                    Sender = MyOperator.Id.ToString(),
                    Tuple = tuple,
                    Receiver = null,
                    ID = OPAndRep.ToString() + id
                };
                try
                {
                    MySemantic(downRep, req);
                    return;
                } catch (SocketException e)
                {
                    ConsoleLog("Downstream is down, rerouting...");
                    lock (DownIpsLock)
                    {
                        MyOperator.DownIps.RemoveAt(index);
                    }
                }
            }
            ConsoleLog("No downstream available.");
        }


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
            if (ProcessedTuplesID.TryGetValue(req.ID, out _))
            {
                return true;
            }
            return false;
        }
    }
}
