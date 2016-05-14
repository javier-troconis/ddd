using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace host
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

    public class StartApplicationCommand 
    {
        public Guid ApplicationId { get; set; }
    }

    public class SubmitApplicationCommand
    {
        public Guid ApplicationId { get; set; }
        public string Submitter { get; set; }
    }
}
