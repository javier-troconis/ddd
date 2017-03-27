using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI.Common.Log;

using infra;

namespace registry
{
    public class Program
    {
        public static void Main(string[] args)
        {
	        EventStoreRegistry.RegisterTopicsStream(
		        new ProjectionManager(
			        EventStoreSettings.ClusterDns,
				        EventStoreSettings.ExternalHttpPort,
				        EventStoreSettings.Username,
				        EventStoreSettings.Password,
				        new ConsoleLogger())).Wait();
        }
    }
}
