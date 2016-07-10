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
        public static readonly int InternalHttpPort = 1113;
        public static readonly int ExternalHttpPort = 1114;
        public static readonly string ClusterDns = "fake.dns";
    }
}
