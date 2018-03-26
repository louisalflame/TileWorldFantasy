using System.Collections;
using System.Collections.Generic;
using System;

namespace Utility
{
    public class Defer : IDisposable
    {
        Action _actions = null;

        public void Add(Action action)
        {
            _actions = (_actions == null) ? action : action + _actions;
        }

        public void Dispose()
        {
            if (_actions != null)
                _actions();
        }
    }
}
