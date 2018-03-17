using System;
using System.Collections;

namespace Utility
{
    public interface IMonad<T>
    {
        T Result { get; }
        Exception Error { get; }
        IEnumerator Do();
    }

    /// <summary>
    /// BlockMoand adapt iterator block to IMoand&lt;T&gt; interface
    /// </summary>
    /// <typeparam name="T">ResultType</typeparam>
    public class BlockMonad<T> : IMonad<T>, IReturn<T> {
        public T Result {get; private set;}
        public Exception Error {get; private set;}

        public BlockMonad(Func<IReturn<T>, IEnumerator> impl)
        {
            _doImpl = impl;
        }

        public void Accept(T result)
        {
            Error = null;
            Result = result;
        }

        public void Fail(Exception error)
        {
            Error = error;
        }

        Func<IReturn<T>, IEnumerator> _doImpl;

        public IEnumerator Do()
        {
            return _doImpl(this);
        }
    }

    public static class Monad
    {
        public static readonly SimpleMonad<None> NoneMonad = new SimpleMonad<None>(default(None));

        public static IMonad<U> Then<T, U>(this IMonad<T> monad, Func<T, IMonad<U>> binder)
        {
            return new BindMonad<T, U, U>(monad, binder, (t, u) => u);
        }

        public static IMonad<None> Then<T>(this IMonad<T> monad, Action<T> binder)
        {
            return new BindMonad<T, None, None>(monad,
                r => NoneMonad, 
                (t, n) => { binder(t); return n; });
        }

        public static IMonad<T> Catch<T>(this IMonad<T> monad, Func<Exception, IMonad<T>> handler)
        {
            return new CatchMonad<T>(monad, handler);
        }

        public static IMonad<U> Map<T, U>(this IMonad<T> monad, Func<T, U> binder)
        {
            return new BindMonad<T, None, U>(monad, result => NoneMonad, (t, n) => binder(t));
        }

        public static IMonad<T[]> WhenAll<T>(params IMonad<T>[] ms)
        {
            return new ConcurrentMonad<T>(ms);
        }

        public static IMonad<Tuple<T1, T2>> WhenAll<T1, T2>(IMonad<T1> m1, IMonad<T2> m2)
        {
            return new ConcurrentMonad<T1, T2>(m1, m2);
        }

        public static IMonad<Tuple<T1, T2, T3>> WhenAll<T1, T2, T3>(IMonad<T1> m1, IMonad<T2> m2, IMonad<T3> m3)
        {
            return new ConcurrentMonad<T1, T2, T3>(m1, m2, m3);
        }
    }

    public static class MonadLinq
    {
        public static IMonad<V> SelectMany<T, U, V>(this IMonad<T> monad, Func<T, IMonad<U>> binder, Func<T, U, V> selector)
        {
            return new BindMonad<T, U, V>(monad, binder, selector);
        }
    }

    /// <summary>
    /// SimpleMoand adapt simple value to IMoand&lt;T&gt interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleMonad<T> : IMonad<T>
    {
        public T Result { get; private set; }
        public Exception Error { get; private set; }
        
        public IEnumerator Do()
        {
            return Coroutine.Empty;
        }

        public SimpleMonad(T result)
        {
            Result = result;
        }

        public SimpleMonad(Exception error)
        {
            Error = error;
        }
    }

    /// <summary>
    /// BindMonad chain two monads. They will be executed sequentially, and the Result of first monad will be passed to binder to generate second monad.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class BindMonad<T, U, V> : IMonad<V>
    {
        public BindMonad(IMonad<T> first, Func<T, IMonad<U>> binder, Func<T, U, V> selector)
        {
            _first = first;
            _binder = binder;
            _selector = selector;
        }

        public V Result
        {
            get;
            private set;
        }

        public Exception Error
        {
            get;
            private set;
        }

        public IEnumerator Do()
        {
            yield return _first.Do();
            if (_first.Error != null)
            {
                Error = _first.Error;
                yield break;
            }

            try
            {
                _second = _binder(_first.Result);
            }
            catch(System.Exception e)
            {
                Error = e;
                yield break;
            }

            yield return _second.Do();
            Error = _second.Error;
            if (Error != null)
                yield break;

            try
            {
                Result = _selector(_first.Result, _second.Result);
            }
            catch (System.Exception e)
            {
                Error = e;
            }
        }

        Func<T, U, V> _selector;
        IMonad<T> _first;
        IMonad<U> _second;
        Func<T, IMonad<U>> _binder;
    }

    /// <summary>
    /// CatchMonad chains a monad and an error handler. If the first one failed with an exception, that exception will be passed to the handler and the returned value becomes the final result.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CatchMonad<T> : IMonad<T>
    {
        public CatchMonad(IMonad<T> first, Func<Exception, IMonad<T>> handler)
        {
            _first = first;
            _handler = handler;
        }

        public T Result
        {
            get;
            private set;
        }

        public Exception Error
        {
            get;
            private set;
        }

        public IEnumerator Do()
        {
            yield return _first.Do();
            if(_first.Error == null){
                Result = _first.Result;
                Error = null;
                yield break;
            }

            var second = _handler(_first.Error);
            yield return second.Do();
            Result = second.Result;
            Error = second.Error;
        }

        IMonad<T> _first;
        Func<Exception, IMonad<T>> _handler;
    }

    /// <summary>
    /// ConcurrentMonad executes monads concurrently. When any monad finished with error, the CorrurentMonad will stop immediately.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public class ConcurrentMonad<T> : IMonad<T[]>
    {
        private readonly IMonad<T>[] _ms;

        public ConcurrentMonad(IMonad<T>[] ms)
        {
            _ms = ms;
        }

        public T[] Result
        {
            get;
            private set;
        }

        public Exception Error
        {
            get;
            private set;
        }


        public IEnumerator Do()
        {
            Executor executor = new Executor();
            using (var defer = new Defer())
            {
                defer.Add(() =>
                {
                    foreach (Coroutine c in executor)
                    {
                        c.Dispose();
                    }
                    executor.Clear();
                });

                for (int i = 0; i < _ms.Length; ++i)
                {
                    executor.Add(_Do(_ms[i]));
                }

                executor.Resume(Coroutine.Delta);
                while (!executor.Finished)
                {
                    if (Error != null)
                    {
                        yield break;
                    }
                    yield return null;
                    executor.Resume(Coroutine.Delta);
                }

                if (Error != null)
                    yield break;
                Result = System.Array.ConvertAll(_ms, m => m.Result);
            }
        }

        public IEnumerator _Do( IMonad<T> m)
        {
            yield return m.Do();
            if (m.Error != null)
                Error = m.Error;
        }
    }

    /// <summary>
    /// ConcurrentMonad executes monads concurrently. When any monad finished with error, the CorrurentMonad will stop immediately.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public class ConcurrentMonad<T1, T2> : IMonad<Tuple<T1, T2>>
    {
        private readonly IMonad<T1> _m1;
        private readonly IMonad<T2> _m2;

        public ConcurrentMonad(IMonad<T1> m1, IMonad<T2> m2)
        {
            _m1 = m1;
            _m2 = m2;
        }


        public Tuple<T1, T2> Result
        {
            get;
            private set;
        }

        public Exception Error
        {
            get;
            private set;
        }

        public IEnumerator Do()
        {
            Executor executor = new Executor();
            using (var defer = new Defer())
            {
                defer.Add(() =>
                {
                    foreach (Coroutine c in executor)
                    {
                        c.Dispose();
                    }
                    executor.Clear();
                });

                executor.Add(_Do(_m1));
                executor.Add(_Do(_m2));

                executor.Resume(Coroutine.Delta);
                while (!executor.Finished)
                {
                    if (Error != null)
                    {
                        yield break;
                    }
                    yield return null;
                    executor.Resume(Coroutine.Delta);
                }
                if (Error != null)
                    yield break;
                Result = new Tuple<T1, T2>(_m1.Result, _m2.Result);
            }
        }

        private IEnumerator _Do<U>(IMonad<U> m)
        {
            yield return m.Do();
            if (m.Error != null)
                Error = m.Error;
        }
    }

    /// <summary>
    /// ConcurrentMonad executes monads concurrently. When any monad finished with error, the CorrurentMonad will stop immediately.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public class ConcurrentMonad<T1, T2, T3> : IMonad<Tuple<T1, T2, T3>>
    {
        private readonly IMonad<T1> _m1;
        private readonly IMonad<T2> _m2;
        private readonly IMonad<T3> _m3;

        public ConcurrentMonad(IMonad<T1> m1, IMonad<T2> m2, IMonad<T3> m3)
        {
            _m1 = m1;
            _m2 = m2;
            _m3 = m3;
        }


        public Tuple<T1, T2, T3> Result
        {
            get;
            private set;
        }

        public Exception Error
        {
            get;
            private set;
        }

        public IEnumerator Do()
        {
            Executor executor = new Executor();
            using (var defer = new Defer())
            {
                defer.Add(() =>
                {
                    foreach (Coroutine c in executor)
                    {
                        c.Dispose();
                    }
                    executor.Clear();
                });

                executor.Add(_Do(_m1));
                executor.Add(_Do(_m2));
                executor.Add(_Do(_m3));

                executor.Resume(Coroutine.Delta);
                while (!executor.Finished)
                {
                    if (Error != null)
                    {
                        yield break;
                    }
                    yield return null;
                    executor.Resume(Coroutine.Delta);
                }
                if (Error != null)
                    yield break;
                Result = new Tuple<T1, T2, T3>(_m1.Result, _m2.Result, _m3.Result);
            }
        }

        private IEnumerator _Do<U>(IMonad<U> m)
        {
            yield return m.Do();
            if (m.Error != null)
                Error = m.Error;
        }
    }

    /// <summary>
    /// ThreadedMonad evaluates a function in a background thread.
    /// </summary>
    /// <typeparam name="T">The return type of background thread.</typeparam>
    public class ThreadedMonad<T> : IMonad<T>
    {
        public T  Result
        {
            get;
            private set;
        }

        public Exception Error
        {
            get;
            private set;
        }

        Func<T> _func;

        public ThreadedMonad( Func<T> func )
        {
            _func = func;
        }

        public IEnumerator Do()
        {
            using (var defer = new Defer())
            {
                var thread = new System.Threading.Thread(() =>
                {
                    try
                    {
                        Result = _func();
                    }
                    catch (Exception e)
                    {
                        Error = e;
                    }
                });

                thread.Start();
                defer.Add(() =>
                {
                    if (thread.IsAlive)
                        thread.Abort();
                });

                while (thread.IsAlive)
                    yield return null;
            } 
        }
    }
}