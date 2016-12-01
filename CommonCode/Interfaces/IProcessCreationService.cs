using CommonCode.Comms;
using CommonCode.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonCode.Interfaces
{
    public interface IProcessCreationService : IRemoteObject
    {
        void Config(string loglevel, string semantics);
        void AddOperator(Operator op);
        Operator getOperator(Tuple<string, int> op_rep);

        void Start(string opid);
        void Interval(string opid, int time);
        void Status();
        void Crash(Tuple<string, int> id);
        void Freeze(Tuple<string, int> id);
        void Unfreeze(Tuple<string, int> id);
    }
}
