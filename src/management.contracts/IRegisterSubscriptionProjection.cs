﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace management.contracts
{
    public interface IRegisterSubscriptionProjection
    {
		string ServiceName { get; }
		string SubscriptionName { get; }
	}
}
