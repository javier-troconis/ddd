using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace infra
{
    public static class StreamNamingConvention
    {
		public static readonly Func<Guid, string> FromIdentity = entityId => entityId.ToString("N").ToLower();
	}
}
