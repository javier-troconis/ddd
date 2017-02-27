using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using shared;
using Elasticsearch.Net;

using Nest;

namespace subscriber
{
    public interface IElasticClientBuilder
    {
        IElasticClientBuilder MapDefaultTypeIndices(Assembly documentAssembly);
        IElasticClient Create();
    }

    public class ElasticClientBuilder : IElasticClientBuilder
    {
        private readonly Uri[] _defaultNodeUris;
        private readonly string _username;
        private readonly string _password;
        private readonly Func<ConnectionSettings, ConnectionSettings> _configure = x => x;

        public ElasticClientBuilder(Uri[] defaultNodeUris, string username, string password)
        {
            _defaultNodeUris = defaultNodeUris;
            _username = username;
            _password = password;
        }

        private ElasticClientBuilder(Uri[] defaultNodeUris, string username, string password, Func<ConnectionSettings, ConnectionSettings> configure)
            : this(defaultNodeUris, username, password)
        {
            _configure = configure;
        }

        public IElasticClientBuilder MapDefaultTypeIndices(Assembly documentAssembly)
        {
            return new ElasticClientBuilder(_defaultNodeUris, _username, _password,
                _configure
                    .ComposeForward(
                        x => x.MapDefaultTypeIndices(
                            y => documentAssembly.GetElasticDocumentTypes().ToList().ForEach(z => y[z] = z.GetElasticIndexName()))));
        }

        public IElasticClient Create()
        {
            var connectionPool = new SniffingConnectionPool(_defaultNodeUris);
            var connectionSettings = new ConnectionSettings(connectionPool)
                .SniffOnStartup(false)
                .ThrowExceptions()
                .BasicAuthentication(_username, _password);
            return new ElasticClient(_configure(connectionSettings));
        }
    }
}
