﻿using CommonCode.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonCode.Comms;
using System.Diagnostics;
using static CommonCode.Comms.DTO;
using System.IO;
using CommonCode.Models;

namespace ProcessCreationService_project
{
    public class ProcessCreationService : MarshalByRefObject, IProcessCreationService
    {
        public const int PORT = 10001;
        public const string NAME = "pcs";

        private Uri MyUri { get; set; }
        private string MyName { get; set; }
        private IPuppet PuppetMaster { get; set; }

        private Dictionary<string, List<IReplica>> replicas;

        private Dictionary<string, List<Operator>> operators;
        private string LoggingLevel { get; set; }
        private string Semantics { get; set; }

        public static string BuildURI(string ip)
        {
            if (!ip.StartsWith("tcp://"))
            {
                ip = "tcp://" + ip;
            }
            return ip + ":" + PORT + "/" + NAME;
        }

        public ProcessCreationService()
        {
            replicas = new Dictionary<string, List<IReplica>>();
            operators = new Dictionary<string, List<Operator>>();
        }

        public string PingRequest()
        {
            return "PONG";
        }

        public void Config(string loglevel, string semantics)
        {
            LoggingLevel = loglevel;
            Semantics = semantics;
            Console.WriteLine("Got LoggingLevel: " + LoggingLevel);
            Console.WriteLine("Got Semantics: " + Semantics);
        }

        public void AddOperator(Operator op)
        {
            Console.WriteLine("Adding new operator with ID: " + op.Id);
            List<Operator> tempOperators = null;
            operators.TryGetValue(op.Id.Item1,out tempOperators);
            if (tempOperators == null)
            {
                tempOperators = new List<Operator>();
                operators.Add(op.Id.Item1, tempOperators); 
            }
            tempOperators.Add(op);
            Process.Start(AppDomain.CurrentDomain.BaseDirectory + "Replica.exe",op.Id.Item1 + " " + op.Id.Item2 + " " + op.Port + " " + op.PCS );

        }

        public bool ProcessTask(DTO blob)
        {
            switch (blob.cmdType)
            {
                //cmdtype used to see what the puppetmaster wants

                case CommandType.PUPPETMASTERINFO:
                    {
                        //hello from puppetmaster, puppetmaster url should be sent on the sender field.

                        PuppetMaster = (IPuppet)Activator.GetObject(typeof(IPuppet), blob.Sender);
                        Console.WriteLine(PuppetMaster.PingRequest());

                        LoggingLevel = blob.Tuple[DTO.LOGGINGLEVEL];
                        Semantics = blob.Tuple[DTO.SEMANTICS];

                        Console.WriteLine("LoggingLevel is " + LoggingLevel);
                        Console.WriteLine("Semantics is " + Semantics);
                        return true;
                    }
                case CommandType.CREATEOPERATOR:
                    {
                        //start a replica, .exe should be in the same directory as the PCS.exe

                        //either pass the arguments here 
                        Process.Start(AppDomain.CurrentDomain.BaseDirectory + "Replica.exe" /*, arguments*/);

                        //, or use a system similar to case 1, replica waits for info which is sent via remoting, 
                        // can be done via a task that tries until the replica is up and receives its info.

                        List<IReplica> mylist = null;

                        //one OPERATOR_ID points to multiple replicas!
                        replicas.TryGetValue("operator_id", out mylist);
                        if(mylist == null)
                        {
                            mylist = new List<IReplica>();
                        }
                        mylist.Add((IReplica) Activator.GetObject(typeof(IReplica), "replica_uri"));
                        replicas.Add("operator_id", mylist);

                        return true;
                    }
                case CommandType.STARTOPERATOR:
                    {
                        List<IReplica> mylist = null;
                        replicas.TryGetValue("operator_id", out mylist);
                        foreach (var replica in mylist)
                        {
                            Console.WriteLine(replica.PingRequest());
                        }
                        return true;
                    }

                default: return false;//do nothing
            }
            
        }

        public Operator getOperator(Tuple<string, int> op_rep)
        {
            List<Operator> ops = operators[op_rep.Item1];
            foreach (var o in ops)
            {
                if (o.Id.Item1 == op_rep.Item1 && o.Id.Item2 == op_rep.Item2)
                    return o;
            }
            return null;
        }
    }
}
