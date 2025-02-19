# Aaron.Akka.Streams.BackpressureMonitor
This package provides access to some experimental [Akka.NET Streams](https://getakka.net/articles/streams/introduction.html) stages that can be used for monitoring occurrences of backpressure inside your Akka.NET applications.

## Installation and Use
To access these stages, install the `Aaron.Akka.Streams.BackpressureMonitor` NuGet package:

```shell
install-package Aaron.Akka.Streams.BackpressureMonitor
```

Next, use the `Aaron.Akka.Streams.Dsl` namespace and call the `BackpressureAlert` extension method on either a `Source<T>` or a `Flow<T>` stage:

```csharp
var source = Source.From(Enumerable.Repeat(0, 10))
    .BackpressureAlert(LogLevel.InfoLevel)
    .RunWith(Sink.Ignore<int>(), Sys.Materializer());
```

You will see the following logs appear in your stream when backpressure is detected between the downstream consumer and the upstream producer above the `BackpressureAlert` stage:

```
[INFO][11/2/2021 7:10:45 PM][Thread 0055][Logic (akka://test)] [BackpressureAlert] Backpressure detected. Measuring duration starting now...
[INFO][11/2/2021 7:10:45 PM][Thread 0055][Logic (akka://test)] [BackpressureAlert] Backpressure relieved. Total backpressure wait time: 00:00:00.0777019
```

### Customizing the `BackpressureAlert` Logs
If you need to raise backpessure alerts in multiple locations, you can change the name of each `BackpressureAlert` stage instance via the `WithAttributes` extension method in Akka.Streams:

```csharp
var source = Source.From(Enumerable.Repeat(0, 3))
    .BackpressureAlert(LogLevel.InfoLevel).WithAttributes(Attributes.CreateName("MyName"))
    .SelectAsync(1, async i =>
    {
        await Task.Delay(100);
        return i;
    })
    .RunWith(Sink.Ignore<int>(), Sys.Materializer());
```

The `Attributes.CreateName("MyName")` call will cause the logs to carry additional context about which alert is firing:

```
[INFO][11/2/2021 7:10:45 PM][Thread 0045][Logic (akka://test)] [MyName-BackpressureAlert] Backpressure detected. Measuring duration starting now...
[INFO][11/2/2021 7:10:45 PM][Thread 0046][Logic (akka://test)] [MyName-BackpressureAlert] Backpressure relieved. Total backpressure wait time: 00:00:00.0470042
```

## Contributing
See [CONTRIBUTING.md](CONTRIBUTING.md) for information about building the project from source and contributing changes.