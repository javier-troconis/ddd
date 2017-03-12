using infra;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using contracts;

using Elasticsearch.Net;

using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

using ImpromptuInterface;

using Nest;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using shared;

namespace subscriber
{
	public class Program : IMessageHandler<IApplicationStarted, Task>, IMessageHandler<IApplicationSubmitted, Task>
	{
		//static async Task<int?> GetDocumentTypeVersion<TDocument>(IElasticClient elasticClient) where TDocument : class
		//{
		//    const string maxVersionQueryKey = "max_version";
		//    const string elasticVersionFieldName = "_version";
		//    var searchResponse = await elasticClient.SearchAsync<TDocument>(s => s
		//        .Size(0)
		//        .Aggregations(agregateSelector => agregateSelector
		//            .Max(maxVersionQueryKey, maxSelector => maxSelector
		//                .Field(elasticVersionFieldName))));
		//    var maxVersionAggregation = searchResponse.Aggs.Max(maxVersionQueryKey);
		//    var maxVersion = maxVersionAggregation.Value;
		//    return (int?)maxVersion;
		//}

		//private static Task MapType<TDocument>(IElasticClient elasticClient) where TDocument : class
		//{
		//    return elasticClient.MapAsync<TDocument>(mapping => mapping
		//        .AutoMap());
		//}

		static async Task IndexAsync<TDocument>(IElasticClient elasticClient, TDocument document, long version) where TDocument : class
		{
			try
			{
				await elasticClient.IndexAsync(document, s => s
					.VersionType(VersionType.External)
					.Version(version)
					.Refresh()
					);
			}
			catch (ElasticsearchClientException ex) when (ex.Response.ServerError.Status == (int)HttpStatusCode.Conflict)
			{
			}
		}

		static async Task UpdateAsync<TDocument>(IElasticClient elasticClient, Guid documentId, Action<TDocument> updateDocument, long expectedVersion) where TDocument : class
		{
			IGetResponse<TDocument> getResponse = await elasticClient.GetAsync<TDocument>(documentId);
			updateDocument(getResponse.Source);
			await IndexAsync(elasticClient, getResponse.Source, expectedVersion + 1);
		}

		private static Func<ResolvedEvent, Task<ResolvedEvent>> Enqueue(TaskQueue queue, string queueName, Func<ResolvedEvent, Task<ResolvedEvent>> handle)
		{
			return async resolvedEvent =>
			{
				await queue.SendToChannelAsync(queueName, () => handle(resolvedEvent));
				return resolvedEvent;
			};
		}

		public static void Main(string[] args)
		{
			var queue = new TaskQueue();

			new EventBus(
				EventStoreSettings.ClusterDns,
				EventStoreSettings.Username,
				EventStoreSettings.Password,
				EventStoreSettings.ExternalHttpPort,
				new ConsoleLogger())
				.RegisterCatchupSubscriber(
					new Program(new ElasticClient()),
					() => Task.FromResult(default(int?)),
					handle => Enqueue(queue, nameof(Program), handle.ComposeForward(_writeCheckpoint.ToAsyncInput())))
				.Start()
				.Wait();

			while (true)
			{

			}
		}

		private static readonly Func<ResolvedEvent, Task<ResolvedEvent>> _writeCheckpoint = resolvedEvent =>
		{
			Console.WriteLine("wrote checkpoint: " + resolvedEvent.OriginalEventNumber);
			return Task.FromResult(resolvedEvent);
		};

		readonly IElasticClient _elasticClient;

		public Program(IElasticClient elasticClient)
		{
			_elasticClient = elasticClient;
		}

		public Task Handle(IApplicationStarted message)
		{
			return Task.CompletedTask;
		}

		public Task Handle(IApplicationSubmitted message)
		{
			return Task.CompletedTask;
		}
	}

	[ElasticsearchType]
	public class TestDocument
	{
		public string Id { get; set; }
		public long Value { get; set; }
	}
}
