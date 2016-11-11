using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonCode.Models
{
    [Serializable]
    public abstract class Command
    {
        public abstract void Execute(Dictionary<string, List<Operator>> doperators);
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

        public override void Execute(Dictionary<string, List<Operator>> doperators)
        {
            throw new NotImplementedException();
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

        public override void Execute(Dictionary<string, List<Operator>> doperators)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class CommandStatus: Command
    {
        public override void Execute(Dictionary<string, List<Operator>> doperators)
        {
            throw new NotImplementedException();
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

        public override void Execute(Dictionary<string, List<Operator>> doperators)
        {
            throw new NotImplementedException();
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

        public override void Execute(Dictionary<string, List<Operator>> doperators)
        {
            throw new NotImplementedException();
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

        public override void Execute(Dictionary<string, List<Operator>> doperators)
        {
            throw new NotImplementedException();
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

        public override void Execute(Dictionary<string, List<Operator>> doperators)
        {
            throw new NotImplementedException();
        }
    }
}
