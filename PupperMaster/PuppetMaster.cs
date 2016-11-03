using PuppetMaster.RemotingInterfaces;
using System;

namespace DADStorm.PuppetMaster
{
    class PuppetMaster
    {
        private byte semantics { get; set; }
        private string[] pcs_ips;
        public PuppetMaster(string[] pcs_ips)
        {
            this.pcs_ips = pcs_ips;
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
