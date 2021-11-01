using System;
using NBench;

namespace Aaron.Akka.Streams.BackpressureMonitor.Tests.Performance
{
    class Program
    {
        static int Main(string[] args)
        {
            return NBenchRunner.Run<Program>();
        }
    }
}
