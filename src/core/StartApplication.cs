using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace core
{
    public struct ApplicationStarted : IEvent
    {

    }

    public static class StartApplication
    {
        public static IEnumerable<IEvent> Apply()
        {
            yield return new ApplicationStarted();
        }
    }
}
