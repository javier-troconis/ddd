using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace infra
{
    public interface ISubscription
    {
	    Task Start();
    }
}
