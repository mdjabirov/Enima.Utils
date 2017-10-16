namespace Enima.Utils.Test {
    public class Pinger : Player {
        public Pinger(IMediator<int> mediator) : base(mediator) {
        }

        public void Start() {
            int message = 2017;
            Send(Topic.Ping, message);
        }

        public void StartAsync() {
            int message = 2017;
            Post(Topic.PingAsync, message);
        }

        public void ManyPings(int count) {
            int message = 2017;
            for (int i = 0; i < count; i++) {
                Send(Topic.Ping, message);
            }
        }

        [Handler(Topic.Pong)]
        private void OnPong(int m) {
            Recd++;
            Send(Topic.Ping, m + 1);
        }

        [Handler(Topic.PongAsync)]
        private void OnPongAsync(int m) {
            Recd++;
            Post(Topic.PingAsync, m + 1);
        }

        // Self-notfy
        [Handler(Topic.Ping)]
        [HandlerAsync(Topic.PingAsync)]
        private void OnPing(int m) {
            Self++;
        }
    }
}
