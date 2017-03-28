namespace eventstore
{
    public static class EventStoreSettings
    {
        public static readonly int InternalHttpPort = 1113;
        public static readonly int ExternalHttpPort = 1114;
        public static readonly string ClusterDns = "fake.dns";
	    public static readonly string Username = "admin";
	    public static readonly string Password = "changeit";
    }
}
