using System;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Implementation;
using Akka.Streams.Implementation.Fusing;
using Akka.Streams.Implementation.Stages;
using Akka.Streams.Stage;

namespace Aaron.Akka.Streams.Dsl
{
    
    public sealed class LogBackpressure<T> : SimpleLinearGraphStage<T>
    {
        private readonly LogLevel _backPressureLogLevel;
        
        private sealed class Logic : InAndOutGraphStageLogic
        {
            private readonly LogBackpressure<T> _stage;
            private readonly LogLevel _backPressureLevel;

            public Logic(LogBackpressure<T> stage) : base(stage.Shape)
            {
                _stage = stage;
                _backPressureLevel = stage._backPressureLogLevel;

                SetHandler(stage.Inlet, this);
                SetHandler(stage.Outlet, this);
            }

            public override void OnPush()
            {
                Push(_stage.Outlet, Grab(_stage.Inlet));
            }

            public override void OnPull()
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Creates a new diagnostic <see cref="LogBackpressure{T}"/> stage.
        /// </summary>
        /// <param name="backPressureLogLevel">The <see cref="LogLevel"/> to use while recording backpressure.</param>
        public LogBackpressure(LogLevel backPressureLogLevel = LogLevel.DebugLevel)
        {
            _backPressureLogLevel = backPressureLogLevel;
            InitialAttributes = Module.
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
