using System;
using System.Threading;
using Enima.Utils;
using NUnit.Framework;

namespace Enima.Utils.Test {
    [TestFixture]
    public class MediatorTest {
        [SetUp]
        public void SetUp() {
            _pinger = new Pinger(mediator);
            _ponger = new Ponger(mediator);
        }

        [TearDown]
        public void TearDown() {
            mediator.RemoveSubscriber(_pinger);
            mediator.RemoveSubscriber(_ponger);
        }

        [Test]
        public void AddRemoveSubscriberTest() {
            Assert.AreEqual(1, _pinger.GetHandlers(Topic.Ping).Count);
            Assert.AreEqual(1, _pinger.GetHandlers(Topic.Pong).Count);
            Assert.AreEqual(1, _ponger.GetHandlers(Topic.Ping).Count);
            Assert.AreEqual(1, _ponger.GetHandlers(Topic.Pong).Count);

            mediator.RemoveSubscriber(_ponger);
            Assert.IsNull(_ponger.GetHandlers(Topic.Ping));
            Assert.IsNull(_ponger.GetHandlers(Topic.Pong));

            _pinger.Start();
            Assert.AreEqual(1, _pinger.Sent);
            Assert.AreEqual(1, _pinger.Self);
            Assert.AreEqual(0, _pinger.Recd);
            Assert.AreEqual(0, _ponger.Recd);
            Assert.AreEqual(0, _ponger.Sent);
            Assert.AreEqual(0, _ponger.Self);

            mediator.RemoveSubscriber(_pinger);
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
        public void PingPongAsyncTest() {
            _pinger.StartAsync();
            Thread.Sleep(3000);
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

        private Mediator<int> mediator = new Mediator<int>();
        private Pinger _pinger;
        private Ponger _ponger;
    }

    public sealed class HandlerAttribute : Attribute, IHandlerAttribute<int> {
        public HandlerAttribute(int topic) {
            _topic = topic;
        }
        public int Topic => _topic;
        private int _topic;
    }

    public sealed class HandlerAsyncAttribute : Attribute, IHandlerAttribute<int> {
        public HandlerAsyncAttribute(int topic) {
            _topic = topic;
        }
        public int Topic => _topic;
        private int _topic;
    }

    public abstract class Player : Subscriber<int> {
        public Player(IMediator<int> mediator) : base(mediator) {
            _mediator = mediator;
        }

        protected void Send<M>(int topic, M message) {
            _sent++;
            _mediator.Send(topic, message);
        }

        protected void Post<M>(int topic, M message) {
            _sent++;
            _mediator.Post(topic, message);
        }

        public long Self => _self;
        public long Sent => _sent;
        public long Recd => _recd;
        protected long _self;
        protected long _sent;
        protected long _recd;

        private IMediator<int> _mediator;
    }

    public class Pinger : Player {
        public Pinger(IMediator<int> mediator) : base(mediator) { }

        public void Start() {
            int message = 2017;
            Send(Topic.Ping, message);
        }

        public void StartAsync() {
            int message = 2017;
            Post(Topic.PingAsync, message);
        }

        public void ManyPings() {
            int message = 2017;
            for (int i = 0; i < 10000000; i++) {
                Send(Topic.Ping, message);
            }
        }

        [Handler(Topic.Pong)]
        private void OnPong(int m) {
            _recd++;
            Send(Topic.Ping, m + 1);
        }

        [Handler(Topic.PongAsync)]
        private void OnPongAsync(int m) {
            _recd++;
            Post(Topic.PingAsync, m + 1);
        }

        // Self-notfy to test Func with return type
        [Handler(Topic.Ping)]
        [HandlerAsync(Topic.PingAsync)]
        private bool OnPing(int m) {
            _self++;
            return true;
        }
    }

    public class Ponger : Player {
        public Ponger(IMediator<int> mediator) : base(mediator) { }

        [Handler(Topic.Ping)]
        private void OnPing(int m) {
            _recd++;
            // stop after a while - stack overflow can occur on sends so limit to small number
            if (_recd > 100) {
                return;
            }
            Send(Topic.Pong, m + 1);
        }

        [Handler(Topic.PingAsync)]
        private void OnPingAsync(int m) {
            _recd++;
            // stop after a while - no fear of a stack overflow so we can play longer
            if (_recd > 100000) {
                return;
            }
            Post(Topic.Pong, m + 1);
        }

        // Self-notify to test Func with return type
        [Handler(Topic.Pong)]
        [HandlerAsync(Topic.PongAsync)]
        private bool OnPong(int m) {
            _self++;
            return true;
        }
    }

    public static class Topic {
        public const int Ping = -1;
        public const int Pong = -2;
        public const int PingAsync = -3;
        public const int PongAsync = -4;
    }
}
