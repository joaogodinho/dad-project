using PuppetMaster.RemotingInterfaces;
using CommonCode.Interfaces;
using System;

namespace DADStorm.PuppetMaster
{
    class PuppetMaster : IPuppet
    {
        private byte semantics { get; set; }
        private string[] pcs_ips;
        
        public PuppetMaster(string[] pcs_ips)
        {
            this.pcs_ips = pcs_ips;
        }

        public void SendMsg(string message)
        {
            throw new NotImplementedException();
        }

        public string pingRequest()
        {
            return "hey, you reached The PuppetMaster";
        }
    }

    delegate void DelLogMsg(string message);

    class PuppetMasterServices : MarshalByRefObject, IPuppetLogger
    {
        public static frmPuppetMaster form;

        public void SendMsg(string message)
        {
            form.Invoke(new DelLogMsg(form.LogMsg), message);
        }
    }
}
