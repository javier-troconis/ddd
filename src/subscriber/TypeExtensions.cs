using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace subscriber
{
    internal static class TypeExtensions
    {
        public static string GetElasticIndexName(this Type documentType)
        {
            return ((documentType.Namespace == null ? string.Empty : documentType.Namespace.Replace(".", "_") + "_") + documentType.Name).ToLower();
        }
    }
}
