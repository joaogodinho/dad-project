using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using CommonCode.Interfaces;
using CommonCode.Comms;

namespace Replica_project
{
    public class Replica : IReplica
    {

        //Add fields as needed
        public string MyId { get; set; }
        public Uri MyUri { get; set; }
        public List<Tuple<string,string>> Replicas { get; set; }
        public string CurrentSemantic { get; set; }
        

        public Replica(string id, string myurl){
            MyId = id;
            MyUri = new Uri(myurl);
        }

        public Task<bool> processRequest(DTO blob)
        {
            throw new NotImplementedException();
        }

        public Task<bool> processResponse(DTO blob)
        {
            throw new NotImplementedException();
        }

        private void mainProcessingCycle(object DTO)
        {

        }

        public string pingRequest()
        {
            return "hey, you reached " + this.MyId + " on uri " + this.MyUri.ToString();
        }
        
    }
}
