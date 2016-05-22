using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EventStore.ClientAPI.SystemData;

namespace infra
{
    public static class EventStoreSettings
    {
		public static readonly UserCredentials Credentials = new UserCredentials("admin", "changeit");
		public static readonly IPEndPoint HttpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2113);
		public static readonly IPEndPoint TcpEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113);
        public static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan WriteTimeout = TimeSpan.FromSeconds(2);
    }
}
