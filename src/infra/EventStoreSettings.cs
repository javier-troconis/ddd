using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace infra
{
    public static class EventStoreSettings
    {
		public static readonly UserCredentials Credentials = new UserCredentials("admin", "changeit");
        public static readonly IPEndPoint InternalHttpEndPoint = new IPEndPoint(IPAddress.Loopback, 1113);
        public static readonly IPEndPoint ExternalHttpEndPoint = new IPEndPoint(IPAddress.Loopback, 1114);
    }
}
