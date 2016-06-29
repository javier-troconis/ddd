using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace infra
{
    public struct NodeConfiguration
    {
        public readonly IPEndPoint InternalHttpEndPoint;
        public readonly IPEndPoint ExternalHttpEndPoint;

        public NodeConfiguration(IPEndPoint internalHttpEndPoint, IPEndPoint externalHttpEndPoint)
        {
            InternalHttpEndPoint = internalHttpEndPoint;
            ExternalHttpEndPoint = externalHttpEndPoint;
        }
    }

    public static class EventStoreSettings
    {
		public static readonly UserCredentials Credentials = new UserCredentials("admin", "changeit");
        public static readonly NodeConfiguration[] NodeConfigurations = 
        {
            new NodeConfiguration(new IPEndPoint(IPAddress.Loopback, 1113), new IPEndPoint(IPAddress.Loopback, 1114)),
            new NodeConfiguration(new IPEndPoint(IPAddress.Loopback, 2113), new IPEndPoint(IPAddress.Loopback, 2114)),
            new NodeConfiguration(new IPEndPoint(IPAddress.Loopback, 3113), new IPEndPoint(IPAddress.Loopback, 3114))
        };
    }
}
