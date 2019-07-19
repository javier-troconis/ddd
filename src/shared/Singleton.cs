using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace shared
{
    public class Singleton<T> where T : class
    {
        private T _instance;
        private readonly object _lock = new object();

        public T GetInstance(Func<T> createInstance)
        {
            if (!Equals(_instance, default(T)))
            {
                return _instance;
            }
            Monitor.Enter(_lock);
            if (Equals(_instance, default(T)))
            {
                var instance = createInstance();
                Interlocked.Exchange(ref _instance, instance);
            }
            Monitor.Exit(_lock);
            return _instance;
        }
    }
}
