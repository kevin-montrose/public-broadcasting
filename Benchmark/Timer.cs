using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    class Timer : IDisposable
    {
        private Stopwatch Watch;

        public Timer(string msg)
        {
            Console.Write(msg+"... ");

            Watch = new Stopwatch();
            Watch.Start();
        }

        public void Dispose()
        {
            Watch.Stop();

            Console.WriteLine(Watch.ElapsedMilliseconds + "ms");
        }
    }
}
