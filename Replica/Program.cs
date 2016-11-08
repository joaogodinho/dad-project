using CommonCode.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace Replica_project
{
    class Program
    {
        private const string NAME = "op";

        static void Main(string[] args)
        {
            Replica ThisReplica = new Replica(args[1],"url");
            ChannelServices.RegisterChannel(new TcpChannel(int.Parse(args[1])), false);
            RemotingServices.Marshal(ThisReplica,"op", typeof(IReplica));
            Console.WriteLine("Replica has been started, waiting commands and inputs");
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }
}
