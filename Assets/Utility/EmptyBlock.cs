using UnityEngine;
using System.Collections;
using System;

namespace Utility
{
    public class EmptyBlock : IEnumerator
    {
        public object Current
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        public bool MoveNext()
        {
            return false;
        }

        public void Reset()
        {
        }
    }
}