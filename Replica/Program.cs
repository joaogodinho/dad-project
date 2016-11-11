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

        private const int REPLICA_OP_ID = 0;
        private const int REPLICA_REP_ID = 1;
        private const int REPLICA_PORT = 2;
        private const int REPLICA_URI = 3;

        static void Main(string[] args)
        {
            Console.Title = args[REPLICA_OP_ID] + args[REPLICA_REP_ID];
            Console.SetWindowSize(70, 15);
            ChannelServices.RegisterChannel(new TcpChannel(int.Parse(args[REPLICA_PORT])), false);
            Replica ThisReplica = new Replica(args[REPLICA_OP_ID],args[REPLICA_URI], new Tuple<string,int>(args[REPLICA_OP_ID],int.Parse(args[REPLICA_REP_ID])));
            RemotingServices.Marshal(ThisReplica,"op", typeof(IReplica));
            Console.WriteLine("Replica has been started, waiting commands and inputs");
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }
}
