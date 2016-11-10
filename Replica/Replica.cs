using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using CommonCode.Interfaces;
using CommonCode.Comms;
using CommonCode.Models;

namespace Replica_project
{
    public class Replica : MarshalByRefObject, IReplica
    {

        //Add fields as needed
        public string MyId { get; set; }
        public Uri MyUri { get; set; }
        public string CurrentSemantic { get; set; }
        public Operator MyOperator { get; set; }


        public Replica(string id, string myurl)
        {
            MyId = id;
            MyUri = new Uri(myurl);
        }

        public bool processRequest(DTO blob)
        {
            Console.WriteLine("Received a tuple from " + blob.Sender);
            ThreadPool.QueueUserWorkItem(mainProcessingCycle, blob);
            return true;
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

        public string pingRequest()
        {
            return "hey, you reached " + this.MyId + " on uri " + this.MyUri.ToString();
        }

    }
}
