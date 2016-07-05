using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace infra
{
    public static class NamingConvention
    {
        public static string Stream(Guid identity)
        {
            return identity.ToString("N").ToLower();
        }

        public static string Subscription<TSubscription>()
        {
            return typeof(TSubscription).Name.ToLower();
        }
    }
}
