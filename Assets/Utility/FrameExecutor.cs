using UnityEngine;
using System.Collections;
using System;

namespace Utility
{
    [SerializeField]
    public class FrameExecutor : MonoBehaviour, IExecutor
    {
        Executor _executor = new Executor();

        public bool Finished
        {
            get
            {
                return _executor.Finished;
            }
        }

        public void Add(IResumable resumable)
        {
            _executor.Add(resumable);
        }

        public void Resume(float delta)
        {
            _executor.Resume(Time.deltaTime);
        }

        void Update()
        {
            Resume(Time.deltaTime);
        }

        public void Destroy()
        {
            GameObject.Destroy(this.gameObject);
        }

        public static FrameExecutor Create()
        {
            var go = new GameObject("Frame_Executor");
            GameObject.DontDestroyOnLoad(go);
            var executor = go.AddComponent<FrameExecutor>();
            return executor;
        }
    }

}