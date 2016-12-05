using CommonCode.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
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
            // Console.SetWindowSize(70, 15);
            ThisProcess = new ProcessCreationService();
            // Creating a custom formatter for a TcpChannel sink chain.
            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            // Creating the IDictionary to set the port on the channel instance.
            IDictionary props = new Hashtable();
            props["port"] = ProcessCreationService.PORT;
            ChannelServices.RegisterChannel(new TcpChannel(props, null, provider), false);
            RemotingServices.Marshal(ThisProcess, ProcessCreationService.NAME, typeof(IProcessCreationService));

            Console.WriteLine("PCS has been started, waiting for instructions from the puppetmaster");
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
    }
}
