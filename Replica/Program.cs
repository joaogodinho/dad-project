using CommonCode.Interfaces;
using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Replica_project
{
    class Program
    {
        private const string NAME = "op";
        private const int TIMEOUT = 1000;

        private enum EArgs
        {
            OP_ID,
            REP_ID,
            PORT,
            URI,
            LOG,
            PM
        }

        static void Main(string[] args)
        {
            Console.Title = args[(int)EArgs.OP_ID] + " #" + args[(int)EArgs.REP_ID];

            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();

            IDictionary props = new Hashtable();
            props["port"] = int.Parse(args[(int) EArgs.PORT]);
            props["timeout"] = TIMEOUT;

            ChannelServices.RegisterChannel(new TcpChannel(props, null, provider), false);

            Replica ThisReplica = new Replica(args[(int)EArgs.REP_ID],
                args[(int)EArgs.URI],
                new Tuple<string,int>(args[(int)EArgs.OP_ID],
                int.Parse(args[(int)EArgs.REP_ID])),
                args[(int)EArgs.LOG],
                args[(int)EArgs.PM]);

            RemotingServices.Marshal(ThisReplica,"op", typeof(IReplica));

            Console.WriteLine("Replica has been started, waiting commands and inputs");
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }
}
