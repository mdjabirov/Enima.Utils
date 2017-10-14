using System;

namespace Enima.Utils {
    public interface IHandlerAttribute<T> {
        T Topic { get; }
    }
}
