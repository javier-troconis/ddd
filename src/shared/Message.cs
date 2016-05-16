using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
    public interface IHeader
    {
        IDictionary<string, object> Header { get; set; }
    }
    public interface IBody<TBody>
    {
        TBody Body { get; set; }
    }

    public class Message<TBody> : IHeader, IBody<TBody>
    {
        public IDictionary<string, object> Header { get; set; }
        public TBody Body { get; set; }
    }
}
