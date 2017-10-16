namespace Enima.Utils.Test {
    public class Ponger : Player {
        public Ponger(IMediator<int> mediator) : base(mediator) {
        }

        [Handler(Topic.Ping)]
        private void OnPing(int m) {
            Recd++;
            // stop after a while - stack overflow can occur on sends so limit to small number
            if (Recd > 100) {
                return;
            }
            Send(Topic.Pong, m + 1);
        }

        [Handler(Topic.PingAsync)]
        private void OnPingAsync(int m) {
            Recd++;
            // stop after a while - no fear of a stack overflow so we can play longer
            if (Recd > 10000) {
                return;
            }
            Post(Topic.Pong, m + 1);
        }

        // Self-notify
        [Handler(Topic.Pong)]
        [HandlerAsync(Topic.PongAsync)]
        private void OnPong(int m) {
            Self++;
        }
    }
}
