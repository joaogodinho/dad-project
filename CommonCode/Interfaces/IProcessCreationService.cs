using CommonCode.Comms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonCode.Interfaces
{
    public interface IProcessCreationService : IRemoteObject
    {
        bool processTask(DTO blob);
    }
}
