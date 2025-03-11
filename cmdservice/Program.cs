using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace cmdservice
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            //if (Environment.UserInteractive)
            //{
            //    //    ServiceBase[] ServicesToRun;
            //    //    ServicesToRun = new ServiceBase[]
            //    //    {
            //    //        new Service1()
            //    //    };
            //    //    ServiceBase.Run(ServicesToRun);
            //    //     Run as console app
            //    Service1 service = new Service1();
            //    service.Start();

            //    Console.WriteLine("Press any key to stop...");
            //    Console.ReadKey();

            //    service.Stopp();
            //}
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
