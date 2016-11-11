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

        static void Main(string[] args)
        {
            Console.Title = ProcessCreationService.NAME.ToUpper();
            Console.SetWindowSize(70, 15);
            ThisProcess = new ProcessCreationService();
            ChannelServices.RegisterChannel(new TcpChannel(ProcessCreationService.PORT), false);
            RemotingServices.Marshal(ThisProcess, ProcessCreationService.NAME, typeof(IProcessCreationService));

            Console.WriteLine("PCS has been started, waiting for instructions from the puppetmaster");
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }
}
