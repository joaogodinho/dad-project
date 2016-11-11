using CommonCode.Interfaces;
using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using CommonCode.Models;
using System.Collections.Generic;
using ProcessCreationService_project;

namespace DADStorm.PuppetMaster
{
    class PuppetMaster : MarshalByRefObject, IPuppet
    {
        private static frmPuppetMaster MyForm;

        public const int PORT = 10000;
        public const string NAME = "puppetmaster";

        public string LoggingLevel { get; set; }
        public string Semantics { get; set; }

        // <URL, PCS>
        private Dictionary<string, IProcessCreationService> DPCS = new Dictionary<string, IProcessCreationService>();
        // <OP_ID, Operator[]>
        private Dictionary<string, List<Operator>> DOperators = new Dictionary<string, List<Operator>>();

        public PuppetMaster(frmPuppetMaster myform)
        {
            MyForm = myform;

            //Register this PuppetMaster
            ChannelServices.RegisterChannel(new TcpChannel(PORT), false);
            RemotingServices.Marshal(this, NAME, typeof(IPuppet));            
        }

        //Add a pcs, test the connection 
        public void AddAndTestPCS(string uri)
        {
            IProcessCreationService pcs = null;

            DPCS.TryGetValue(uri, out pcs);
            // New PCS endpoint
            if(pcs == null)
            {
                try {
                    pcs = (IProcessCreationService)Activator.GetObject(typeof(IProcessCreationService), uri);
                    WriteMessage("Checking PCS@" + uri + "...");
                    var response = pcs.PingRequest();
                    WriteMessage("Got response: " + response);
                    DPCS.Add(uri, pcs);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        internal void SendConfigToPCS()
        {
            // Notify each PCS of the configuration
            foreach (KeyValuePair<string, IProcessCreationService> pcs in DPCS)
            {
                WriteMessage("Sending config to " + pcs.Key + "...");
                pcs.Value.Config(LoggingLevel, Semantics);
            }
            // Notify each PCS of their Operators
            foreach (KeyValuePair<string, List<Operator>> node in DOperators)
            {
                foreach (Operator operatorRep in node.Value)
                {
                    WriteMessage("Sending opeartor " + operatorRep.Id + " to " + operatorRep.PCS + "...");
                    // If this should ever be null, someone screwed up, not me....
                    IProcessCreationService pcs = DPCS[operatorRep.PCS];
                    pcs.AddOperator(operatorRep);
                }
            }
        }

        private enum EConfig
        {
            ID = 0,
            INPUT_KEYWORD,
            INPUT_VALUE,
            REP_KEYWORD,
            REP_VALUE,
            ROUTE_KEYWORD,
            ROUTE_VALUE,
            ADDR_KEYWORD,
            ADDR_START
        }

        internal void ParseAndAddOperator(string s)
        {
            string[] values = s.Split(' ');

            string opID = values[(int)EConfig.ID];

            Debug.Assert(values[(int)EConfig.INPUT_KEYWORD].ToLower() == "input_ops");
            string input = values[(int)EConfig.INPUT_VALUE];

            Debug.Assert(values[(int)EConfig.REP_KEYWORD].ToLower() == "rep_fact");
            int replicas = int.Parse(values[(int)EConfig.REP_VALUE]);

            Debug.Assert(values[(int)EConfig.ROUTE_KEYWORD].ToLower() == "routing");
            // TODO Change to deal with other routing methods
            Tuple<string, string> routing = new Tuple<string, string>("primary", "");

            // Create the operator's list here, since we can now know how many there will be
            List<Operator> opList = new List<Operator>();
            Debug.Assert(values[(int)EConfig.ADDR_KEYWORD].ToLower() == "address");
            List<Uri> uris = new List<Uri>();
            for (int i = 0; i < replicas; i++)
            {
                string[] temp = values[(int)EConfig.ADDR_START + i].Split(',');
                Uri uri = new Uri(values[(int)EConfig.ADDR_START + i].Split(',')[0]);
                uris.Add(uri);
                // Seems like a good time to try and connect to the PCS that should be on this IP
                AddAndTestPCS(ProcessCreationService.BuildURI(uri.Host));
                // Add this operator to the list
                opList.Add(new Operator(
                    new Tuple<string, int>(opID, i),
                    ProcessCreationService.BuildURI(uri.Host),
                    input,
                    uri.Port,
                    routing
                ));
            }

            int op_keyword = (int)EConfig.ADDR_START + replicas;
            Debug.Assert(values[op_keyword].ToLower() == "operator_spec");
            OperatorSpec operation;
            string[] filterParams;
            switch (values[op_keyword + 1].ToLower())
            {
                case "filter":
                    filterParams = values[op_keyword + 2].Split(',');
                    operation = new OperatorFilter(int.Parse(filterParams[0]), filterParams[1], filterParams[2]);
                    break;
                case "custom":
                    filterParams = values[op_keyword + 2].Split(',');
                    operation = new OperatorCustom(filterParams[0], filterParams[1], filterParams[2]);
                    break;
                case "uniq":
                    int field = int.Parse(values[op_keyword + 2]);
                    operation = new OperatorUniq(field);
                    break;
                case "count":
                    operation = new OperatorCount();
                    break;
                case "dup":
                    operation = new OperatorDup();
                    break;
                default:
                    throw new Exception("Unknown Operation.");
            }

            // Set the operation for the operator
            foreach (Operator op in opList)
            {
                op.Spec = operation;
            }

            DOperators.Add(opID, opList);

            List<Operator> receivingOperators = null;
            DOperators.TryGetValue(input, out receivingOperators);
            // So it doesn't blow up if the operators are declared in a wrong order
            if (receivingOperators!= null)
            {
                foreach (var item in receivingOperators)
                {
                    // Tell the upstream guy where he should connect to
                    foreach (var uri in uris)
                    {
                        item.DownIps.Add(uri);
                    }
                }
            }
        }

        public void ParseCommand(string sCommand)
        {
            string[] values = sCommand.Split(' ');
            Command command;
            switch (values[0].ToLower())
            {
                case "start":
                    command = new CommandStart(values[1]);
                    break;
                case "interval":
                    command = new CommandInterval(values[1], int.Parse(values[2]));
                    break;
                case "status":
                    command = new CommandStatus();
                    break;
                case "crash":
                    command = new CommandCrash(values[1], int.Parse(values[2]));
                    break;
                case "freeze":
                    command = new CommandFreeze(values[1], int.Parse(values[2]));
                    break;
                case "unfreeze":
                    command = new CommandUnfreeze(values[1], int.Parse(values[2]));
                    break;
                case "wait":
                    command = new CommandWait(int.Parse(values[2]));
                    break;
                default:
                    throw new Exception("Unknown command.");
            }
            command.Execute(DOperators);
        }

        public void SendMsg(string message)
        {
            ThreadPool.QueueUserWorkItem(WriteMessage, message);
        }

        private void WriteMessage(object state)
        {
            MyForm.Invoke(new DelLogMsg(MyForm.LogMsg), (string)state);
        }

        public string PingRequest()
        {
            return "PONG";
        }
        
        delegate void DelLogMsg(string message);
    
    }
}
