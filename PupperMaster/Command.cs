using CommonCode.Interfaces;
using CommonCode.Models;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DADStorm.PuppetMaster 
{
    [Serializable]
    public abstract class Command
    {
        // TODO Refactor the execute, code is repeated
        // TODO Maybe be more verbal when executing commands on PM GUI
        public abstract void Execute(PuppetMaster pm);
    }

    public abstract class CommandID : Command
    {
        public string ID { get; set; }
        public CommandID(string id)
        {
            ID = id;
        }
    }

    [Serializable]
    public class CommandStart : CommandID
    {
        public CommandStart(string id) : base(id)
        {
        }

        public override void Execute(PuppetMaster pm)
        {
            // If this blows up, the config is using the wrong OPs, cause my code is fabulous
            List<string> pcs = pm.DOperators[ID].Select(x => x.PCS).Distinct().ToList();
            foreach (string process in pcs)
            {
                // Get the PCS reference
                IProcessCreationService pcs_process = pm.DPCS[process];
                // Fire away
                pcs_process.Start(ID);
            }
        }
    }

    [Serializable]
    public class CommandCrash: CommandID
    {
        public int RepID { get; set; }

        public CommandCrash(string id, int repid) : base(id)
        {
            RepID = repid;
        }

        public override void Execute(PuppetMaster pm)
        {
            // If this blows up, the config is using the wrong OPs, cause my code is fabulous
            List<Operator> operators = pm.DOperators[ID];
            foreach (Operator op in operators)
            {
                // We could just broadcast the crash to all OPs and let the validation be
                // done on the receiving side, but this is cleaner.
                if (op.Id.Item2 == RepID)
                {
                    // Get the PCS reference
                    IProcessCreationService pcs = pm.DPCS[op.PCS];
                    // Fire away
                    pcs.Crash(new Tuple<string, int>(ID, RepID));
                }
            }
        }
    }

    [Serializable]
    public class CommandStatus: Command
    {
        public override void Execute(PuppetMaster pm)
        {
            // If this blows up, the config is using the wrong OPs, cause my code is fabulous
            foreach (KeyValuePair<String, IProcessCreationService> pcs in pm.DPCS)
            {
                // You get a status, he gets a status, everyone gets a status
                pcs.Value.Status();
            }
        }
    }

    [Serializable]
    public class CommandInterval: CommandID
    {
        public int Interval { get; set; }

        public CommandInterval(string id, int interval) : base(id)
        {
            Interval = interval;
        }

        public override void Execute(PuppetMaster pm)
        {
            // If this blows up, the config is using the wrong OPs, cause my code is fabulous
            List<string> pcs = pm.DOperators[ID].Select(x => x.PCS).Distinct().ToList();
            foreach (string process in pcs)
            {
                // Get the PCS reference
                IProcessCreationService pcs_process = pm.DPCS[process];
                // Fire away
                pcs_process.Interval(ID,Interval);
            }
        }
    }

    [Serializable]
    public class CommandFreeze: CommandID
    {
        public int RepID{ get; set; }

        public CommandFreeze(string id, int repid) : base(id)
        {
            RepID = repid;
        }

        public override void Execute(PuppetMaster pm)
        {
            // If this blows up, the config is using the wrong OPs, cause my code is fabulous
            List<Operator> operators = pm.DOperators[ID];
            foreach (Operator op in operators)
            {
                // We could just broadcast the crash to all OPs and let the validation be
                // done on the receiving side, but this is cleaner.
                if (op.Id.Item2 == RepID)
                {
                    // Get the PCS reference
                    IProcessCreationService pcs = pm.DPCS[op.PCS];
                    // Fire away
                    pcs.Freeze(new Tuple<string, int>(ID, RepID));
                }
            }
        }
    }

    [Serializable]
    public class CommandWait: Command
    {
        public int Time { get; set; }

        public CommandWait(int time)
        {
            Time = time;
        }

        public override void Execute(PuppetMaster pm)
        {
            Thread.Sleep(Time);
        }
    }

    [Serializable]
    public class CommandUnfreeze: CommandID
    {
        public int RepID { get; set; }

        public CommandUnfreeze(string id, int repid) : base(id)
        {
            RepID = repid;
        }

        public override void Execute(PuppetMaster pm)
        {
            // If this blows up, the config is using the wrong OPs, cause my code is fabulous
            List<Operator> operators = pm.DOperators[ID];
            foreach (Operator op in operators)
            {
                // We could just broadcast the crash to all OPs and let the validation be
                // done on the receiving side, but this is cleaner.
                if (op.Id.Item2 == RepID)
                {
                    // Get the PCS reference
                    IProcessCreationService pcs = pm.DPCS[op.PCS];
                    // Fire away
                    pcs.Unfreeze(new Tuple<string, int>(ID, RepID));
                }
            }
        }
    }
}
