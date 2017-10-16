#if NET40
using System.Threading.Tasks;
#endif
using NUnit.Framework;

namespace Enima.Utils.Test {
    [TestFixture]
    public class MediatorTest {
        [SetUp]
        public void SetUp() {
            _pinger = new Pinger(_mediator);
            _ponger = new Ponger(_mediator);
            _receiver = new Receiver(_mediator);
        }

        [TearDown]
        public void TearDown() {
            _mediator.RemoveSubscriber(_pinger);
            _mediator.RemoveSubscriber(_ponger);
            _mediator.RemoveSubscriber(_receiver);
        }

        [Test]
        public void SendReceiveTest() {
            Receiver r = _receiver;
            _mediator.SendAll(Topic.TopicTwoDoubles, 1.0, 2.5);
            Assert.AreEqual(3.5, r.TwoDoubles);
            _mediator.SendAll(Topic.TopicTwoStrings, "Hello", "World");
            Assert.AreEqual("Hello World", r.TwoStrings);
            _mediator.SendAll<object>(Topic.TopicAnyObjects, "Hello", "World", '!');
            Assert.AreEqual(3, r.AnyObjects.Length);
            _mediator.SendAll<object>(Topic.TopicAnyObjects, "Hello", 2.5);
            Assert.AreEqual(2, r.AnyObjects.Length);
            _mediator.Send(Topic.TopicEmpty);
            Assert.AreEqual(2, r.EmptyCalled);
        }

        [Test]
        public void AddRemoveSubscriberTest() {
            Assert.AreEqual(1, _pinger.GetHandlers(Topic.Ping).Count);
            Assert.AreEqual(1, _pinger.GetHandlers(Topic.Pong).Count);
            Assert.AreEqual(1, _ponger.GetHandlers(Topic.Ping).Count);
            Assert.AreEqual(1, _ponger.GetHandlers(Topic.Pong).Count);

            _mediator.RemoveSubscriber(_ponger);
            Assert.IsNull(_ponger.GetHandlers(Topic.Ping));
            Assert.IsNull(_ponger.GetHandlers(Topic.Pong));

            _pinger.Start();
            Assert.AreEqual(1, _pinger.Sent);
            Assert.AreEqual(1, _pinger.Self);
            Assert.AreEqual(0, _pinger.Recd);
            Assert.AreEqual(0, _ponger.Recd);
            Assert.AreEqual(0, _ponger.Sent);
            Assert.AreEqual(0, _ponger.Self);

            _mediator.RemoveSubscriber(_pinger);
            Assert.IsNull(_pinger.GetHandlers(Topic.Ping));
            Assert.IsNull(_pinger.GetHandlers(Topic.Pong));
        }

        [Test]
        public void PingPongTest() {
            _pinger.Start();
            Assert.AreEqual(_pinger.Sent, _ponger.Recd);
            Assert.AreEqual(_pinger.Recd, _ponger.Sent);
            Assert.AreEqual(_pinger.Sent, _pinger.Self);
            Assert.AreEqual(_ponger.Sent, _ponger.Self);
        }

        [Test]
        public void PerformanceTest() {
            _pinger.ManyPings();
            Assert.AreEqual(_pinger.Sent, _pinger.Self);
        }

#if NET40
        [Test]
        public void PingPongAsyncTest() {
            _pinger.StartAsync();
            Thread.Sleep(1000);
            Assert.AreEqual(_pinger.Sent, _ponger.Recd);
            Assert.AreEqual(_pinger.Recd, _ponger.Sent);
            Assert.AreEqual(_pinger.Sent, _pinger.Self);
            Assert.AreEqual(_ponger.Sent, _ponger.Self);
        }

        [Test]
        public void TestPostParallel() {
            Task[] tasks = _mediator.PostParallel(Topic.TopicParallel, 41);
            Task.WaitAll(tasks);
            Assert.AreEqual(41 * 1000000, _receiver.TimesLooped);
            Assert.AreEqual(165580141, _receiver.FibonacciNumber);
        }

        private Mediator40<int> _mediator = new Mediator40<int>();
#else
        private Mediator<int> _mediator = new Mediator<int>();
#endif

        private Pinger _pinger;
        private Ponger _ponger;
        private Receiver _receiver;
    }
}
