using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace host
{
    public interface IHeader<THeader>
    {
        THeader Header { get; set; }
    }


    public interface IBody<TBody>
    {
        TBody Body { get; set; }
    }

    public class Message<THeader, TBody> : IHeader<THeader>, IBody<TBody> where THeader : new() where TBody : new()
    {
        public THeader Header { get; set; } = new THeader();
        public TBody Body { get; set; } = new TBody();

        public override string ToString()
        {
            return typeof(TBody).Name;
        }
    }

    public class CommandHeader
    {
        public Guid TenantId { get; set; }
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
