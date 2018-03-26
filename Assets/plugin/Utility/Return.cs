using System;

namespace Utility
{
    public interface IReturn<T>
    {
        void Accept(T result);
        void Fail(Exception error);
    }
}

