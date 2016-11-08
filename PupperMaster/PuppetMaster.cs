using CommonCode.Interfaces;
using System;
using System.Threading;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Tcp;
using CommonCode.Comms;
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
            throw new NotImplementedException();
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
