using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace shared
{
    public class Singleton<T>
    {
        private T _instance;
        private readonly object _lock = new object();

        public T GetInstance(Func<T> createInstance)
        {
            if (!Equals(_instance, default(T)))
            {
                return _instance;
            }
            lock (_lock)
            {
                if (Equals(_instance, default(T)))
                {
                    _instance = createInstance();
                }
            }
            return _instance;
        }
    }
}
