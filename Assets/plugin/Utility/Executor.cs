using System;
using System.Collections;
using System.Collections.Generic;

namespace Utility
{
    public interface IResumable
    {
        bool Finished { get; }
        void Resume(float delta);
    }

    public interface IExecutor : IResumable
    {
        void Add(IResumable resumable);
    }

    public class Executor : IExecutor, ICollection<IResumable>
    {
        private List<IResumable> _resumables = new List<IResumable>();
		
		public bool Finished { get { return _resumables.Count == 0; } }
		
        public bool Empty { get { return _resumables.Count == 0; } }

        public int Count
        {
            get { return _resumables.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public Executor()
        {
            // empty
        }

        public void Resume(float delta)
        {
            for(int i = _resumables.Count-1; i >= 0; --i){
                _resumables[i].Resume(delta);
            }

            _resumables.RemoveAll(r => r.Finished);
        }

        public void Add(IResumable resumable)
        {
            _resumables.Add(resumable);
        }

        public void Remove(IResumable resumable)
        {
            _resumables.Remove(resumable);
        }

        public void Clear()
        {
            _resumables.Clear();
        }

        public IEnumerator Join()
        {
            Resume(Coroutine.Delta);

            while (!Empty){
                yield return null;
                Resume(Coroutine.Delta);
            }
        }
		
		public delegate bool Condition();
		
		public IEnumerator JoinWhile(Condition callback)
		{
			while(callback()){
				yield return null;
				Resume(Coroutine.Delta);
			}
		}

        public IEnumerator TimedJoin(float wait_time)
        {
            while(wait_time > 0 && !Empty){
                yield return null;
                wait_time -= Coroutine.Delta;
                Resume(Coroutine.Delta);
            }
        }

        public bool Contains(IResumable item)
        {
            return _resumables.Contains(item);
        }

        public void CopyTo(IResumable[] array, int arrayIndex)
        {
            _resumables.CopyTo(array, arrayIndex);
        }

        bool ICollection<IResumable>.Remove(IResumable item)
        {
            return _resumables.Remove(item);
        }

        IEnumerator<IResumable> IEnumerable<IResumable>.GetEnumerator()
        {
            return _resumables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _resumables.GetEnumerator();
        }

        public List<IResumable>.Enumerator GetEnumerator()
        {
            return _resumables.GetEnumerator();
        }
    }

    public static class ExcutorExtension
    {
        public static Coroutine Add( this IExecutor executor, IEnumerator handle)
        {
            Coroutine c = new Coroutine(handle);
            executor.Add(c);
            return c;
        }
    }
}

