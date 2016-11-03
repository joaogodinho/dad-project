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
}
