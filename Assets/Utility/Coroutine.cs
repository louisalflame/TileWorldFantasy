using System;
using System.Collections;
using System.Collections.Generic;

namespace Utility
{
    public class Coroutine : IResumable, IDisposable
    { 
        public interface IOperation
        {
            void Execute(Coroutine coroutine);
        }

        public bool Finished {
            get
            {
                return _block == null && _next == null;
            }
        }

        Stack<IEnumerator> _stack = null;
        IEnumerator _block = null;
        Coroutine _next = null;

        public static readonly IEnumerator Empty = new EmptyBlock();

        [ThreadStatic]
        static float _delta;

        public static float Delta {
            get
            {
                return _delta;
            }
            private set
            {
                _delta = value;
            }
        }

        public Coroutine(IEnumerator block)
        {
            _block = block;
        }

        public void Resume(float delta)
        {
            float originalDelta = Delta;
            Delta = delta;

            if (_block != null)
                _Resume();

            _ResumeSiblings();

            Delta = originalDelta;
        }

        private void _Resume()
        {
            while(true)
            {
                if (!_block.MoveNext())
                {
                    _DisposeBlock(_block);
                    if (_stack != null &&_stack.Count > 0)
                    {
                        _block = _stack.Pop();
                        continue;
                    }
                    else
                    {
                        _block = null;
                        break;
                    }
                }

                var c = _block.Current;
                if (c == null)
                    break;

                var subBlock = c as IEnumerator;
                if (subBlock != null){
                    if (_stack == null)
                        _stack = new Stack<IEnumerator>();
                    _stack.Push(_block);
                    _block = subBlock;
                    continue;
                }

                var operation = c as IOperation;
                if( operation != null )
                {
                    operation.Execute(this);
                    continue;
                }
                throw new Exception("Return type is not ether an iterator or an operation");
            }
        }

        private void _ResumeSiblings()
        {
            for (Coroutine curr = _next, prev = this; curr != null;)
            {
                curr._Resume();
                if (curr._block == null)
                    prev._next = curr._next;
                else
                    prev = curr;
                curr = curr._next;
            }
        }

        private void _Dispose()
        {
            if (_block != null)
            {
                _DisposeBlock(_block);
                _block = null;
            }

            if( _stack != null )
            {
                foreach (var block in _stack)
                {
                    _DisposeBlock(block);
                }
                _stack.Clear();
            }
        }

        public void Dispose()
        {
            for( var curr = this; curr != null; curr = curr._next)
            {
                curr._Dispose();
            }
        }

        static void _DisposeBlock( IEnumerator block )
        {
            IDisposable disposable = block as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }

        public static IEnumerator Join(IResumable resumable)
        {
            resumable.Resume(Coroutine.Delta);
            while (!resumable.Finished){
                yield return null;
                resumable.Resume(Coroutine.Delta);
            }
        }

        public static IEnumerator Sleep(float remain)
        {
            while(remain > 0){
                yield return null;
                remain -= Coroutine.Delta;
            }
        }

        public static Coroutine.IOperation Become(IEnumerator block)
        {
            return new BecomeOperation(block);
        }

        [System.Obsolete("Use Exceutor instead")]
        public static Coroutine.IOperation Spawn(IEnumerator block)
        {
            var op = new SpawnOperation(block);
            return op;
        }

        private class BecomeOperation : Coroutine.IOperation
        {

            public BecomeOperation(IEnumerator block)
            {
                _target = block;
            }

            IEnumerator _target;

            public void Execute(Coroutine coroutine)
            {
                _DisposeBlock(coroutine._block);
                coroutine._block = _target;
            }
        }

        private class SpawnOperation : Coroutine.IOperation
        {
            Coroutine _target;

            public SpawnOperation(IEnumerator block)
            {
                _target = new Coroutine(block);
            }

            public void Execute(Coroutine coroutine)
            {
                _target._next = coroutine._next;
                coroutine._next = _target;
            }

            public IEnumerator Join()
            {
                return _Wait(_target);
            }

            static private IEnumerator _Wait(Coroutine coroutine)
            {
                while (coroutine._block != null)
                {
                    yield return null;
                }
            }
        }
    }

    public static class ResumableExtensions
    {
        public static IEnumerator Join(this IResumable resumable)
        {
            return Coroutine.Join(resumable);
        }
    }
}

