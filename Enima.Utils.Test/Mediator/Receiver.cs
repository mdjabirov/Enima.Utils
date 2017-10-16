namespace Enima.Utils.Test {
    public class Receiver : Subscriber<int> {
        public Receiver(IMediator<int> mediator) : base(mediator) { }

        [Handler(Topic.TopicTwoStrings)]
        private void OnTwoStrings(params string[] s) {
            TwoStrings = string.Concat(s[0], " ", s[1]);
        }

        [Handler(Topic.TopicTwoDoubles)]
        private void OnTwoDoubles(params double[] d) {
            TwoDoubles = d[0] + d[1];
        }

        [Handler(Topic.TopicAnyObjects)]
        private void OnAnyObjects(params object[] objects) {
            AnyObjects = new object[objects.Length];
            for (int i = 0; i < objects.Length; i++) {
                AnyObjects[i] = objects[i];
            }
        }

        [Handler(Topic.TopicEmpty)]
        private void OnEmpty1() {
            EmptyCalled++;
        }

        [Handler(Topic.TopicEmpty)]
        private void OnEmpty2() {
            EmptyCalled++;
        }

        [Handler(Topic.TopicParallel)]
        private void LoopMillionTimes(int n) {
            int i;
            for (i = 0; i < n * 1000000; i++) ;
            TimesLooped = i;
        }

        [Handler(Topic.TopicParallel)]
        private void Fibonacci(int n) {
            if (n <= 2) {
                FibonacciNumber = 1;
            }
            ulong prev = 1, next = 1, tmp;
            for (int i = 2; i < n; i++) {
                tmp = next;
                next = next + prev;
                prev = tmp;
            }
            FibonacciNumber = next;
        }

        public string TwoStrings { get; private set; }
        public double TwoDoubles { get; private set; }
        public object[] AnyObjects { get; private set; }
        public int EmptyCalled { get; private set; }
        public int TimesLooped { get; private set; }
        public ulong FibonacciNumber { get; private set; }
    }
}
