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
        private ConcurrentDictionary<string, byte> BeingProcessedTuplesID { get; set; } = new ConcurrentDictionary<string, byte>();
        private List<IReplica> OtherReplicas = new List<IReplica>();
        private const int PROCESS_TIMEOUT = 2000;
        private Object TupleCounterLock = new Object();
        private Object BeingProcessedLock = new Object();
        private Object OtherReplicasLock = new object();
        private bool ExactlyFlag = false;
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
            SetRouting(MyOperator.Routing);

            foreach (string url in MyOperator.ReplicasUris)
            {
                OtherReplicas.Add((IReplica)Activator.GetObject(typeof(IReplica), url));
            }
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
                    MySemantic = ExactlyOnce;
                    ExactlyFlag = true;
                    break;
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

                List<List<string>> result = new List<List<string>>();
                if (ExactlyFlag)
                {
                    if (AreOtherProcessing(dto))
                    {
                        result = ProcessingOperation(dto);
                    }
                    else
                    {
                        // Add the tuple to the input again, in case the guy processing crashes
                        InBuffer.Add(dto);
                        // Check next tuple
                        continue;
                    }
                } else
                {
                    result = ProcessingOperation(dto);
                }

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
                });

                if (IInterval != 0)
                {
                    Thread.Sleep(IInterval);
                }
            }
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

        // Makes sure tuple with the given ID is not being processed elsewhere
        private bool AreOtherProcessing(DTO dto)
        {
            byte _;
            // Ignore ID if already processed
            // This garantees exactly-once for same replica
            if (ProcessedTuplesID.TryGetValue(dto.ID, out _)) { return false; }

            // Set the tuple as being processed
            BeingProcessedTuplesID.TryAdd(dto.ID, 0);

            // If any other replica is processing this tuple
            foreach (IReplica replica in OtherReplicas)
            {
                try
                {
                    if (replica.BeingProcessed(dto.ID))
                    {
                        return false;
                    }
                } catch (SocketException e)
                {
                    ConsoleLog("One other replica died!");
                    lock (OtherReplicasLock)
                    {
                        OtherReplicas.Remove(replica);
                    }
                } catch (Exception e)
                {
                    ConsoleLog("Unexpected exception in AreOtherProcessing:");
                    ConsoleLog(e.ToString());
                    throw;
                }
            }
            return true;
        }

        // Identical to AtLeastOnce, but changes processed and beingprocessed status, as well as
        // broadcasting to other replicas
        private void ExactlyOnce(IReplica downReplica, DTO req)
        {
            AtLeastOnce(downReplica, req);
            // Notify other replicas tuple was processed
            foreach (IReplica replica in OtherReplicas)
            {
                try
                {
                    replica.SetAsProcessed(req.ID);
                } catch (SocketException e)
                {
                    ConsoleLog("One other replica died!");
                    lock (OtherReplicasLock)
                    {
                        OtherReplicas.Remove(replica);
                    }
                } catch (Exception e)
                {
                    ConsoleLog("Unexpected exception in ExactlyOnce:");
                    ConsoleLog(e.ToString());
                    throw;
                }
            }

            ProcessedTuplesID.TryAdd(req.ID, 0);
            byte _;
            lock (BeingProcessedLock)
            {
                BeingProcessedTuplesID.TryRemove(req.ID, out _);
            }
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

        public bool processRequest(DTO blob)
        {
            ConsoleLog("Received a tuple: " + blob.ID);
            InBuffer.Add(blob);
            return true;
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

        public bool BeingProcessed(string id)
        {
            byte _;
            bool status = false;
            // Don't allow touching on being processed if someone is making questions
            lock (BeingProcessedLock)
            {
                status = BeingProcessedTuplesID.TryGetValue(id, out _);
            }
            return status;
        }

        public void SetAsProcessed(string id)
        {
            ProcessedTuplesID.TryAdd(id, 0);
        }
    }
}
