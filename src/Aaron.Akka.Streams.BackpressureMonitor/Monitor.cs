using System;
using System.Linq;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Implementation;
using Akka.Streams.Implementation.Fusing;
using Akka.Streams.Implementation.Stages;
using Akka.Streams.Stage;

namespace Aaron.Akka.Streams.Dsl
{
    public static class BackpressureExtensions
    {
        public static IFlow<T, TMat> BackpressureMonitor<T, TMat>(this IFlow<T, TMat> flow,
            LogLevel backPressureLogLevel = LogLevel.DebugLevel)
        {
            return flow.Via(new LogBackpressure<T>(backPressureLogLevel));
        }
        
        public static Source<T, TMat> BackpressureMonitor<T, TMat>(this Source<T, TMat> flow, 
            LogLevel backPressureLogLevel = LogLevel.DebugLevel)
        {
            return flow.Via(new LogBackpressure<T>(backPressureLogLevel));
        }
    }
    
    public sealed class LogBackpressure<T> : SimpleLinearGraphStage<T>
    {
        private readonly LogLevel _backPressureLogLevel;
        
        private sealed class Logic : InAndOutGraphStageLogic
        {
            private readonly LogBackpressure<T> _stage;
            private readonly LogLevel _backPressureLevel;
            private bool _isBackpressured = false;
            private DateTimeOffset? _backPressureStart = null;

            public Logic(LogBackpressure<T> stage) : base(stage.Shape)
            {
                _stage = stage;
                _backPressureLevel = stage._backPressureLogLevel;

                SetHandler(stage.Inlet, this);
                SetHandler(stage.Outlet, this);
            }

            public override void OnPush()
            {
                // Data is being pushed by producer - here we check if an outlet is ready
                if(IsAvailable(_stage.Outlet))
                    Push(_stage.Outlet, Grab(_stage.Inlet)); // no backpressure
                else // backpressure
                {
                    if (!_isBackpressured)
                    {
                        // backpressure detected for the first time
                        _isBackpressured = true;
                        _backPressureStart = DateTimeOffset.UtcNow;
                        Log.Log(_backPressureLevel, "Backpressure detected. Measuring duration starting now...");
                    }
                }
            }

            public override void OnPull()
            {
                Pull(_stage.Inlet);
                if (_isBackpressured)
                {
                    _isBackpressured = false;
                    var duration = DateTimeOffset.UtcNow - _backPressureStart;
                    _backPressureStart = null;
                    Log.Log(_backPressureLevel, "Backpressure relieved. Total backpressure wait time: {0}", duration);
                }
            }
        }

        /// <summary>
        /// Creates a new diagnostic <see cref="LogBackpressure{T}"/> stage.
        /// </summary>
        /// <param name="backPressureLogLevel">The <see cref="LogLevel"/> to use while recording backpressure.</param>
        public LogBackpressure(LogLevel backPressureLogLevel = LogLevel.DebugLevel)
        {
            _backPressureLogLevel = backPressureLogLevel;
            InitialAttributes = Attributes.CreateName($"[BackPressureMon][IN:{Inlet.Name}]-->[OUT:{Outlet.Name}]");
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected override Attributes InitialAttributes { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="inheritedAttributes">TBD</param>
        /// <returns>TBD</returns>
        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this);

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString() => "LogBackpressure";

        
    }
}
