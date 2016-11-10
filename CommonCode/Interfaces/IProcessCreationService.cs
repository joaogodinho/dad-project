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
        bool ProcessTask(DTO blob);
    }
}
