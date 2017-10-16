using System;

namespace Enima.Utils.Test {
    public sealed class HandlerAsyncAttribute : Attribute, IHandlerAttribute<int> {
        public HandlerAsyncAttribute(int topic) {
            Topic = topic;
        }

        public int Topic { get; private set; }
    }
}
