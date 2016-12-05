using CommonCode.Interfaces;
using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Replica_project
{
    class Program
    {
        private const string NAME = "op";

        private enum EArgs
        {
            OP_ID,
            REP_ID,
            PORT,
            URI
        }

        static void Main(string[] args)
        {
            Console.Title = args[(int)EArgs.OP_ID] + " #" + args[(int)EArgs.REP_ID];
            //Console.SetWindowSize(70, 15);
            ChannelServices.RegisterChannel(new TcpChannel(int.Parse(args[(int)EArgs.PORT])), false);
            Replica ThisReplica = new Replica(args[(int)EArgs.REP_ID], args[(int)EArgs.URI], new Tuple<string,int>(args[(int)EArgs.OP_ID],int.Parse(args[(int)EArgs.REP_ID])),"light");
            RemotingServices.Marshal(ThisReplica,"op", typeof(IReplica));
            Console.WriteLine("Replica has been started, waiting commands and inputs");
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }
}
