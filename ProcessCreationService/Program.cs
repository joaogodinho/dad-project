using CommonCode.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace ProcessCreationService_project
{
    class Program
    {
        private static ProcessCreationService ThisProcess;

        private const string NAME = "PCS";

        static void Main(string[] args)
        {
            Console.Title = NAME;

            ThisProcess = new ProcessCreationService();
            ChannelServices.RegisterChannel(new TcpChannel(10001), false);
            RemotingServices.Marshal(ThisProcess, NAME, typeof(IProcessCreationService));

            Console.WriteLine("PCS has been started, waiting for instructions from the puppetmaster");
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }
}
