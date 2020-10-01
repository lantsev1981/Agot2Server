using GameService;
using System;
using System.IO;
using System.ServiceProcess;

namespace Agot2Server
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new AGOT2Service()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
