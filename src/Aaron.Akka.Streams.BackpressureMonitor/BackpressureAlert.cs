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
        /// <summary>
        /// Detects backpressure within a 40ms window.
        /// </summary>
        private static readonly TimeSpan DefaultBackPressureThreshold = TimeSpan.FromMilliseconds(40);
        
        public static IFlow<T, TMat> BackpressureAlert<T, TMat>(this IFlow<T, TMat> flow,
            LogLevel backPressureLogLevel = LogLevel.DebugLevel, TimeSpan? backpressureThreshold = null)
        {
            return flow.Via(new BackpressureAlert<T>(backpressureThreshold ?? DefaultBackPressureThreshold, backPressureLogLevel));
        }
        
        public static Source<T, TMat> BackpressureAlert<T, TMat>(this Source<T, TMat> flow, 
            LogLevel backPressureLogLevel = LogLevel.DebugLevel, TimeSpan? backpressureThreshold = null)
        {
            return flow.Via(new BackpressureAlert<T>(backpressureThreshold ?? DefaultBackPressureThreshold, backPressureLogLevel));
        }
    }
    
    public sealed class BackpressureAlert<T> : SimpleLinearGraphStage<T>
    {
        private readonly LogLevel _backPressureLogLevel;
        private readonly TimeSpan _backPressureThreshold;
        
        private sealed class Logic : TimerGraphStageLogic, IInHandler, IOutHandler
        {
            private readonly BackpressureAlert<T> _stage;
            private readonly LogLevel _backPressureLevel;
            private readonly string _stageName;
            private bool _waitingDemand = true;
            private bool _isBackpressured = false;
            private readonly TimeSpan _pressureThreshold;
            private long _backPressureDeadline = 0;
            private long _backPressureStart = 0;

            public Logic(BackpressureAlert<T> stage, string stageName) : base(stage.Shape)
            {
                _stage = stage;
                _stageName = stageName;
                _pressureThreshold = stage._backPressureThreshold;
                _backPressureLevel = stage._backPressureLogLevel;

                SetHandler(stage.Inlet, this);
                SetHandler(stage.Outlet, this);
            }

            public void OnPush()
            {
                Push(_stage.Outlet, Grab(_stage.Inlet));
                _waitingDemand = true;
                _backPressureDeadline = DateTimeOffset.UtcNow.Ticks + _pressureThreshold.Ticks;
            }

            public void OnUpstreamFinish() => CompleteStage();

            public void OnUpstreamFailure(Exception e) => FailStage(e);

            public void OnPull()
            {
                Pull(_stage.Inlet);
                _waitingDemand = false;
                
                if (_isBackpressured)
                {
                    _isBackpressured = false;
                    var duration = TimeSpan.FromTicks(DateTimeOffset.UtcNow.Ticks - _backPressureStart);
                    _backPressureStart = 0L;
                    
                    Log.Log(_backPressureLevel, "[{0}] Backpressure relieved. Total backpressure wait time: {1}", _stageName, duration);
                }
            }
            
            public void OnDownstreamFinish()
            {
                CompleteStage();
            }

            protected override void OnTimer(object timerKey)
            {
                if (!_isBackpressured && (_waitingDemand && _backPressureDeadline - DateTimeOffset.UtcNow.Ticks < 0))
                {
                    Log.Log(_backPressureLevel, "[{0}] Backpressure detected. Measuring duration starting now...", _stageName);
                    _isBackpressured = true;
                    _backPressureStart = DateTimeOffset.UtcNow.Ticks;
                }
            }

            public override void PreStart()
            {
               ScheduleRepeatedly(Timers.GraphStageLogicTimer, Timers.IdleTimeoutCheckInterval(_stage._backPressureThreshold));
            }
        }

        /// <summary>
        /// Creates a new diagnostic <see cref="BackpressureAlert{T}"/> stage.
        /// </summary>
        /// <param name="backPressureThreshold">The threshold we use for testing for backpressure. Defaults to 40ms.</param>
        /// <param name="backPressureLogLevel">The <see cref="LogLevel"/> to use while recording backpressure.</param>
        public BackpressureAlert(TimeSpan backPressureThreshold, LogLevel backPressureLogLevel = LogLevel.DebugLevel)
        {
            _backPressureThreshold = backPressureThreshold;
            _backPressureLogLevel = backPressureLogLevel;
            InitialAttributes = Attributes.CreateName($"BackpressureAlert");
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
        protected override GraphStageLogic CreateLogic(Attributes inheritedAttributes) => new Logic(this, inheritedAttributes.GetNameLifted());

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString() => $"BackpressureAlert<{typeof(T)}>";

        
    }
}
