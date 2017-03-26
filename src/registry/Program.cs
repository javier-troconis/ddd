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
	        Task.WhenAll(Enumerable.Range(0, 9).Select(async x =>
	        {
				try
				{
					await EventStoreRegistry.CreateTopicsProjection(
						new ProjectionManager(
							EventStoreSettings.ClusterDns,
							EventStoreSettings.ExternalHttpPort,
							EventStoreSettings.Username,
							EventStoreSettings.Password,
							new ConsoleLogger()));
				}
				catch(Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
	        })).Wait();
				


        }
    }
}
