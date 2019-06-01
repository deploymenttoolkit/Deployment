using DeploymentToolkit.Utils;
using System;
using System.Threading;

namespace DeploymentToolkit.Restart
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Restarting in 10 Seconds ...");
            for(var i = 10; i != 0; i--)
            {
                Console.WriteLine(i);
                Thread.Sleep(1000);
            }
            Console.WriteLine("Restarting ...");

            PowerUtil.Restart();
        }
    }
}
