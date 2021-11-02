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
    public class BackpressureAlertSpecs : TestKit
    {
        public BackpressureAlertSpecs(ITestOutputHelper output) 
            : base(output:output){}
        
        [Fact]
        public async Task ShouldNotLogWithoutBackpressure()
        {
            await EventFilter.Info(contains: "backpressure").ExpectAsync(0, async () =>
            {
                var source = Source.From(Enumerable.Repeat(0, 10))
                    .BackpressureAlert(LogLevel.InfoLevel)
                    .RunWith(Sink.Ignore<int>(), Sys.Materializer());

                await source;
            });
        }
        
        [Fact]
        public async Task ShouldLogWithBackpressure()
        {
            await WithinAsync(TimeSpan.FromSeconds(10), async () =>
            {
                await EventFilter.Info(contains: "backpressure").ExpectAsync(4, RemainingOrDefault, async () =>
                {
                    var source = Source.From(Enumerable.Repeat(0, 3))
                        .BackpressureAlert(LogLevel.InfoLevel)
                        .SelectAsync(1, async i =>
                        {
                            await Task.Delay(100);
                            return i;
                        })
                        .RunWith(Sink.Ignore<int>(), Sys.Materializer());

                    await source;
                });
            });
        }
        
        [Fact]
        public async Task ShouldLogWithBackpressureWithCustomName()
        {
            await WithinAsync(TimeSpan.FromSeconds(10), async () =>
            {
                await EventFilter.Info(contains: "MyName").ExpectAsync(4, RemainingOrDefault, async () =>
                {
                    var source = Source.From(Enumerable.Repeat(0, 3))
                        .BackpressureAlert(LogLevel.InfoLevel).WithAttributes(Attributes.CreateName("MyName"))
                        .SelectAsync(1, async i =>
                        {
                            await Task.Delay(100);
                            return i;
                        })
                        .RunWith(Sink.Ignore<int>(), Sys.Materializer());

                    await source;
                });
            });
        }
    }
}
