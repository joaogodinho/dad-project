﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonCode.Comms
{
    [Serializable]
    public class DTO
    {
        //add fields as desired,
        

        public DTO(CommandType cmd, string sender, string receiver, List<string> tuple, string id)
        {
            Sender = sender;
            Receiver = receiver;
            Tuple = new List<string>();
            cmdType = cmd;
            ID = id;
        }

        public DTO()
        {

        }


        //Replica that makes this request
        public string Sender { get; set; }

        //Replica that receives this request
        public string Receiver { get; set; }

        //values to be used by the Operator in the receiving replica
        public List<string> Tuple { get; set; }

        public CommandType cmdType { get; set; }
        public string ID { get; set; }

        public enum CommandType : int { PUPPETMASTERINFO = 1, CREATEOPERATOR, STARTOPERATOR, }



        public const int LOGGINGLEVEL = 0;
        public const int SEMANTICS = 1;

    }
}
