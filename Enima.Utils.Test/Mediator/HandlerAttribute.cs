using System;

namespace Enima.Utils.Test {
    public sealed class HandlerAttribute : Attribute, IHandlerAttribute<int> {
        public HandlerAttribute(int topic) {
            Topic = topic;
        }

        public int Topic { get; private set; }
    }
}
