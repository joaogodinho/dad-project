using CommonCode.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonCode.Comms;
using System.Diagnostics;
using static CommonCode.Comms.DTO;
using System.IO;

namespace ProcessCreationService_project
{
    public class ProcessCreationService : MarshalByRefObject, IProcessCreationService
    {
        private Uri MyUri { get; set; }
        private string MyName { get; set; }
        private IPuppet PuppetMaster { get; set; }
        private Dictionary<string, List<IReplica>> replicas;


        public ProcessCreationService()
        {
            replicas = new Dictionary<string, List<IReplica>>();
        }

        public string pingRequest()
        {
            return "hey you reached a process creation service";
        }

        public bool processTask(DTO blob)
        {
            switch (blob.cmdType)
            {
                //cmdtype used to see what the puppetmaster wants

                case CommandType.PUPPETMASTERINFO:
                    {
                        //hello from puppetmaster, puppetmaster url should be sent on the sender field.

                        PuppetMaster = (IPuppet)Activator.GetObject(typeof(IPuppet), blob.Sender);
                        Console.WriteLine(PuppetMaster.pingRequest());
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
                            Console.WriteLine(replica.pingRequest());
                        }
                        return true;
                    }

                default: return false;//do nothing
            }
            
        }
    }
}
