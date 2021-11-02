using System;
using System.Linq;
using System.Threading.Tasks;
using Aaron.Akka.Streams.Dsl;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.TestKit.Xunit2;
using Xunit;
using Xunit.Abstractions;

namespace Aaron.Akka.Streams.BackpressureMonitor.Tests
{
    public class LogBackpressureSpecs : TestKit
    {
        public LogBackpressureSpecs(ITestOutputHelper output) 
            : base(output:output){}
        
        [Fact]
        public async Task ShouldNotLogWithoutBackpressure()
        {
            await EventFilter.Info(contains: "backpressure").ExpectAsync(0, async () =>
            {
                var source = Source.From(Enumerable.Repeat(0, 10))
                    .BackpressureMonitor(LogLevel.InfoLevel)
                    .RunWith(Sink.Ignore<int>(), Sys.Materializer());

                await source;
            });
           
        }
    }
}
