namespace Enima.Utils.Test {
    public abstract class Player : Subscriber<int> {
        public Player(IMediator<int> mediator) : base(mediator) {
            _mediator = mediator;
        }

        protected void Send<M>(int topic, M message) {
            Sent++;
            _mediator.Send(topic, message);
        }

        protected void Post<M>(int topic, M message) {
#if NET40
            Sent++;
            _mediator.Post(topic, message);
#endif
        }

        public long Self { get; protected set; }
        public long Sent { get; protected set; }
        public long Recd { get; protected set; } 

        private IMediator<int> _mediator;
    }
}
