using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Nest;

namespace subscriber
{
    internal static class AssemblyExtensions
    {
        public static IEnumerable<Type> GetElasticDocumentTypes(this Assembly documentsAssembly)
        {
            return documentsAssembly
                .GetTypes()
                .Where(y => y
                    .GetCustomAttributes<ElasticsearchTypeAttribute>(true).Any());
        }
    }
}
