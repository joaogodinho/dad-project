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
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Collections;

namespace DADStorm.PuppetMaster
{
    public class PuppetMaster : MarshalByRefObject, IPuppet
    {
        private static frmPuppetMaster MyForm;

        public const int PORT = 10000;
        public const string NAME = "puppetmaster";

        public string LoggingLevel { get; set; }
        public string Semantics { get; set; }

        // <URL, PCS>
        public Dictionary<string, IProcessCreationService> DPCS = new Dictionary<string, IProcessCreationService>();
        // <OP_ID, Operator[]>
        public Dictionary<string, List<Operator>> DOperators = new Dictionary<string, List<Operator>>();

        public PuppetMaster(frmPuppetMaster myform)
        {
            MyForm = myform;

            //Register this PuppetMaster
            // Creating a custom formatter for a TcpChannel sink chain.
            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            // Creating the IDictionary to set the port on the channel instance.
            IDictionary props = new Hashtable();
            props["port"] = PORT;
            ChannelServices.RegisterChannel(new TcpChannel(props, null, provider), false);
            RemotingServices.Marshal(this, NAME, typeof(IPuppet));            
        }

        // Resets the PM state when loading new config
        public void Reset()
        {
            DPCS = new Dictionary<string, IProcessCreationService>();
            DOperators = new Dictionary<string, List<Operator>>();
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
            // Notify each PCS of the configuration and tell them to reset
            foreach (KeyValuePair<string, IProcessCreationService> pcs in DPCS)
            {
                WriteMessage("Sending config to " + pcs.Key + "...");
                pcs.Value.Config(LoggingLevel, Semantics);
                pcs.Value.Reset();
            }
            // Notify each PCS of their Operators
            foreach (KeyValuePair<string, List<Operator>> node in DOperators)
            {
                foreach (Operator operatorRep in node.Value)
                {
                    WriteMessage("Sending operator " + operatorRep.Id + " to " + operatorRep.PCS + "...");
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
            // Trim just in case the file comes from a windows(\r\n), he says while programming this lovely language in visual studio, lul.
            string[] values = s.Split(' ').Select(p => p.Trim()).ToArray<string>();

            string opID = values[(int)EConfig.ID];

            Debug.Assert(values[(int)EConfig.INPUT_KEYWORD].ToLower() == "input_ops");
            string input = values[(int)EConfig.INPUT_VALUE];

            Debug.Assert(values[(int)EConfig.REP_KEYWORD].ToLower() == "rep_fact");
            int replicas = int.Parse(values[(int)EConfig.REP_VALUE]);

            Debug.Assert(values[(int)EConfig.ROUTE_KEYWORD].ToLower() == "routing");
            // Defaults to primary routing if none of the if's match
            Tuple<string, string> routing = new Tuple<string, string>("primary", "");
            string routing_val = values[(int)EConfig.ROUTE_VALUE].ToLower();
            if (routing_val == "random")
            {
                routing = new Tuple<string, string>("random", "");
            }
            else if (routing_val.StartsWith("hashing"))
            {
                string id = routing_val.Split('(')[1].Split(')')[0];
                routing = new Tuple<string, string>("hashing", id);
            }

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
                
            }
            int replica_id = 0;
            foreach (var uri in uris)
            {
                opList.Add(new Operator(
                    new Tuple<string, int>(opID, replica_id++),
                    ProcessCreationService.BuildURI(uri.Host),
                    input,
                    uri.Port,
                    routing,
                    uris.Select(x => x.ToString()).Except(new string[] { uri.ToString() }).ToList()
                    )
                );
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
            // So it doesn't blow up for the first OP
            // OPs need to be ordered in config file, or this won't work
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
            IProcessCreationService pcs;
            string[] values = sCommand.Split(' ');
            Command command;
            switch (values[0].ToLower())
            {
                case "start":
                    command = new CommandStart(values[1]);
                    List<Operator> operators = DOperators[values[1]];
                    foreach (Operator op in operators)
                    {
                        // Get the PCS reference
                        pcs = DPCS[op.PCS];
                    }
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
                    command = new CommandWait(int.Parse(values[1]));
                    break;
                default:
                    throw new Exception("Unknown command.");
            }
            command.Execute(this);
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
