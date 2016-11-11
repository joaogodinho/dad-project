using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using CommonCode.Interfaces;
using CommonCode.Comms;
using CommonCode.Models;
using System.Collections.Concurrent;
using System.IO;

namespace Replica_project
{
    public class Replica : MarshalByRefObject, IReplica
    {

        //Add fields as needed
        public string MyId { get; set; }
        public Uri MyUri { get; set; }
        public string CurrentSemantic { get; set; }
        public Operator MyOperator { get; set; }
        public ConcurrentQueue<DTO> InBuffer { get; set; }
        public IProcessCreationService PCS { get; set; }
        public Tuple<string,int> OPAndRep { get; set; }

        public Replica(string id, string myurl, Tuple<string,int> op_rep)
        {
            MyId = id;
            MyUri = new Uri(myurl);
            InBuffer = new ConcurrentQueue<DTO>();
            OPAndRep = op_rep;
            PCS = (IProcessCreationService)Activator.GetObject(typeof(IProcessCreationService), myurl);
            MyOperator = PCS.getOperator(OPAndRep);
        }

        public bool processRequest(DTO blob)
        {
            Console.WriteLine("Received a tuple from " + blob.Sender);
            ThreadPool.QueueUserWorkItem(mainProcessingCycle, blob);
            return true;
        }

        public void ReadFile()
        {
            Stream stream = null;
            if ((stream = File.Open(AppDomain.CurrentDomain.BaseDirectory + MyOperator.Input,FileMode.Open)) != null)
            {
                using (stream)
                {
                    string test = (new StreamReader(stream)).ReadToEnd();
                    foreach (string s in test.Split('\n'))
                    {
                        string[] tuple = s.Split(' ');
                        DTO dto = new DTO()
                        {
                            Sender = MyUri.ToString(),
                            Tuple = tuple,
                            Receiver = MyOperator.DownIps[0].ToString()
                        };
                        mainProcessingCycle(dto);
                    }
                }
            }
        }
        
        private void mainProcessingCycle(object blob)
        {
            DTO dto = (DTO)blob;
            Console.WriteLine("Now processing tuple from " + dto.Sender + " tuple is : " + dto.Tuple);
            string[] result = MyOperator.Spec.processTuple(dto.Tuple);
            Console.WriteLine("Finished processing tuple from " + dto.Sender + " result was : " + result);
            //routing is primary for now
            Uri replicaIp = MyOperator.DownIps[0];
            IReplica replica = (IReplica)Activator.GetObject(typeof(IReplica), replicaIp.ToString());

            DTO request = new DTO()
            {
                Sender = MyUri.ToString(),
                Tuple = result,
                Receiver = replicaIp.ToString()
            };
            Console.WriteLine("Now Sending the request Downstream to Replica @ " + request.Receiver);
            var requestResult = replica.processRequest(request);
            Console.WriteLine("Replica @ " + request.Receiver + " has received the request and said : " + requestResult);
        }

        public string PingRequest()
        {
            return "hey, you reached " + this.MyId + " on uri " + this.MyUri.ToString();
        }

    }
}
