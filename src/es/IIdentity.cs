using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
	public interface IIdentity
	{
		Guid Id { get; }
	}
}
