using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dad_project.Comms
{
    [Serializable]
    public class DTO
    {
        //add fields as desired,
        

        public DTO(string sender, string receiver, string[] tuple)
        {
            Sender = sender;
            Receiver = receiver;
            Tuple = tuple;
        }


        //Replica that makes this request
        public string Sender { get; private set; }

        //Replica that receives this request
        public string Receiver { get; private set; }

        //values to be used by the Operator in the receiving replica
        public string[] Tuple { get; private set; }


    }
}
