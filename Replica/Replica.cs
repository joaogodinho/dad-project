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
        private EStatus CurrStatus { get; set; } = EStatus.RUNNING;
        private int IInterval { get; set; } = 0;
        // private ConcurrentQueue<string[]> InBuffer { get; set; } = new ConcurrentQueue<string[]>();
        private ConcurrentQueue<string[]> OutBuffer { get; set; } = new ConcurrentQueue<string[]>();

        //Add fields as needed
        public string MyId { get; set; }
        public Uri MyUri { get; set; }
        public string CurrentSemantic { get; set; }
        public static Operator MyOperator { get; set; }
        // This should just be a tuple? One for input, another for output and make a thread just to push the tuples out
        public ConcurrentQueue<DTO> InBuffer { get; set; }
        public IProcessCreationService PCS { get; set; }
        public Tuple<string,int> OPAndRep { get; set; }

        public Replica(string id, string myurl, Tuple<string,int> op_rep, string loglevel)
        {
            MyId = id;
            MyUri = new Uri(myurl);
            InBuffer = new ConcurrentQueue<DTO>();
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
                // Is this thread safe?
                if (CurrStatus == EStatus.RUNNING )
                {
                    DTO tuple = null;
                    if (InBuffer.TryDequeue(out tuple))
                        mainProcessingCycle(tuple);

                    if (IInterval != 0)
                    {
                        Thread.Sleep(IInterval);
                    }
                }
                else Monitor.Wait(MyOperator);
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
            InBuffer.Enqueue(blob);
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
                            if (CurrStatus != EStatus.RUNNING)
                                Monitor.Wait(MyOperator);
                            else mainProcessingCycle(dto);
                        }
                    }
                }
            }
            
        }
        
        private void mainProcessingCycle(object blob)
        {
            DTO dto = (DTO)blob;
            Console.WriteLine("Now processing tuple from " + dto.Sender + " tuple is : " + dto.Tuple.ToString());
            List<List<string>> result = MyOperator.Spec.processTuple(dto.Tuple);
            if (result.Count == 0) return;
            else
                foreach (List<string> tuple in result)
                {
                    Console.WriteLine("Finished processing tuple from " + dto.Sender + " result was : " + result.ToString());
                    //routing is primary for now
                    if (MyOperator.DownIps.Count > 0)
                    {
                        DTO request = new DTO()
                        {
                            Sender = dto.Sender,
                            Tuple = tuple,
                            Receiver = MyOperator.DownIps[0].ToString()
                        };
                        IReplica replica = (IReplica)Activator.GetObject(typeof(IReplica), dto.Receiver);
                        Console.WriteLine("Now Sending the request Downstream to Replica @ " + request.Receiver);
                        var requestResult = replica.processRequest(request);
                        Console.WriteLine("Replica @ " + request.Receiver + " has received the request and said : " + requestResult);
                    }
                }
        }

        public string PingRequest()
        {
            return "hey, you reached " + this.MyId + " on uri " + this.MyUri.ToString();
        }

        public void Start()
        {
            CurrStatus = EStatus.RUNNING;
            Task.Run(() => Monitor.Pulse(MyOperator));
        }

        public void Interval(int time)
        {
            Debug.Assert(time >= 0);
            IInterval = time;
        }
        

        public void Crash()
        {
            Task.Run(() => Process.GetCurrentProcess().Kill());
        }

        public void Freeze()
        {
            CurrStatus = EStatus.FROZEN;
        }

        public void Unfreeze()
        {
            CurrStatus = EStatus.RUNNING;
            Task.Run(() => Monitor.Pulse(MyOperator));
        }

        public string Status()
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
            Console.WriteLine(result);
            return result;
        }
    }
}
