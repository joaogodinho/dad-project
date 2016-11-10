using CommonCode.Interfaces;
using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using CommonCode.Comms;
using CommonCode.Models;
using static CommonCode.Comms.DTO;
using System.Collections.Generic;

namespace DADStorm.PuppetMaster
{
    class PuppetMaster : MarshalByRefObject, IPuppet
    {
        private static frmPuppetMaster MyForm;

        private const string NAME = "puppetmaster";

        public string LoggingLevel { get; set; }
        public string Semantics { get; set; }

        private Dictionary<string, IProcessCreationService> PcsDictionary = new Dictionary<string, IProcessCreationService>();

        private Dictionary<string, List<Operator>> Operators = new Dictionary<string, List<Operator>>();


        public PuppetMaster(frmPuppetMaster myform)
        {
            MyForm = myform;

            //Register this PuppetMaster
            ChannelServices.RegisterChannel(new TcpChannel(10000), false);
            RemotingServices.Marshal(this, NAME, typeof(IPuppet));            
        }

        //Add a pcs, test the connection just for debugging, they should be reliable and not a fault point
        public bool AddPCS(string pcs_ip)
        {
            IProcessCreationService pcs = null;
            PcsDictionary.TryGetValue(pcs_ip, out pcs);
            if(pcs == null)
            {
                try {
                    pcs = (IProcessCreationService)Activator.GetObject(typeof(IProcessCreationService), pcs_ip);
                    WriteMessage("Sending ping to pcs @ " + pcs_ip);
                    var response = pcs.pingRequest();
                    WriteMessage("Response was: " + response);
                    PcsDictionary.Add(pcs_ip, pcs);
                }
                catch (Exception e)
                {
                    this.SendMsg("failed to add pcs @ " + pcs_ip + " Error was " + e.Message);
                    return false;
                }
            }
            return true;
        }

        public void notifyPcsOfPuppetMaster()
        {
            foreach (string pcs_ip in PcsDictionary.Keys)
            {
                WriteMessage("Notifying pcs @ " + pcs_ip);
                DTO message = new DTO()
                {
                    cmdType = CommandType.PUPPETMASTERINFO,
                    Sender = "tcp://127.0.0.1:10000/puppetmaster",
                    Receiver = pcs_ip,
                    Tuple = new string[] { LoggingLevel, Semantics }
                };
                var response = PcsDictionary[pcs_ip].processTask(message);
                WriteMessage("Response was from pcs @ " + pcs_ip + " was: " + response);
            }
        }

        internal void ParseAndAddOperator(string s)
        {
            Operator op = new Operator();
            string[] values = s.Split(' ');

            op.Id = values[0];

            Debug.Assert(values[1].ToLower() == "input_ops");
            op.Input = values[2];

            Debug.Assert(values[3].ToLower() == "rep_fact");
            // TODO Use replicas variable for final submission
            int replicas = int.Parse(values[4]);

            Debug.Assert(values[5].ToLower() == "routing");
            // TODO Change to deal with other routing methods
            op.Routing = new Tuple<string, string>("primary", "");

            Debug.Assert(values[7].ToLower() == "address");
            // TODO Change to deal with multiple replicas
            List<Uri> uris = new List<Uri>();
            for (int i = 0; i < replicas; i++)
            {
                string[] temp = values[8 + i].Split(',');
                uris.Add(new Uri(values[8 + i].Split(',')[0]));
            }
            op.Port = uris[0].Port;

            Debug.Assert(values[8 + replicas].ToLower() == "operator_spec");
            OperatorSpec operation;
            string[] filterParams;
            switch (values[8 + replicas + 1].ToLower())
            {
                case "filter":
                    filterParams = values[8 + replicas + 2].Split(',');
                    operation = new OperatorFilter(int.Parse(filterParams[0]), filterParams[1], filterParams[2]);
                    break;
                case "custom":
                    filterParams = values[8 + replicas + 2].Split(',');
                    operation = new OperatorCustom(filterParams[0], filterParams[1], filterParams[2]);
                    break;
                case "uniq":
                    int field = int.Parse(values[8 + replicas + 2]);
                    operation = new OperatorUniq(field);
                    break;
                case "dup":
                    operation = new OperatorDup();
                    break;
                default:
                    throw new Exception("Unknown Operation.");
            }
            op.Spec = operation;

            // TODO Change to create list with all replicas
            List<Operator> opList = new List<Operator>();
            opList.Add(op);
            Operators.Add(op.Id, opList);

            List<Operator> receivingOperators = null;
            Operators.TryGetValue(op.Input, out receivingOperators);
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

        internal void verifyConfiguration()
        {
            LoggingLevel = string.IsNullOrEmpty(LoggingLevel) ? "light" : LoggingLevel;
            Semantics = string.IsNullOrEmpty(Semantics) ? "at-most-once" : Semantics;
        }

        public void SendMsg(string message)
        {
            ThreadPool.QueueUserWorkItem(WriteMessage, message);
        }

        private void WriteMessage(object state)
        {
            MyForm.Invoke(new DelLogMsg(MyForm.LogMsg), (string)state);
        }

        public string pingRequest()
        {
            return "hey, you reached The PuppetMaster";
        }
        
        delegate void DelLogMsg(string message);
    
    }
}
