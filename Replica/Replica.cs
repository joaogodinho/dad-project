using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using dad_project.Interfaces;
using dad_project;
using dad_project.Comms;

namespace dad_project
{
    public class Replica :ReplicaInterface
    {

        //Add fields as needed
        public string MyId { get; set; }
        public List<Tuple<string,string>> Replicas { get; set; }
        public string CurrentSemantic { get; set; }
        public Operator MyOperator { get; set; }
        

        public Replica(Operator myOperator){
            MyOperator = myOperator;
            CurrentSemantic = myOperator.Id;

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

    }
}
