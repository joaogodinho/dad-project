using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppetMaster.RemotingInterfaces
{
    public interface IPuppetLogger
    {
        void SendMsg(string message);
    }
}
