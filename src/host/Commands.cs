using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace host
{
    public interface IHeader
    {
        Header Header { get; set; }
    }

    public interface IBody<TBody>
    {
        TBody Body { get; set; }
    }

    public class Message<TBody> : IHeader, IBody<TBody>
    {
        public Header Header { get; set; }
        public TBody Body { get; set; }
    }

    public struct Header
    {
        public Guid TenantId { get; set; }
    }
   

    public struct StartApplicationCommand 
    {
        public Guid ApplicationId { get; set; }
    }

    public struct SubmitApplicationCommand
    {
        public Guid ApplicationId { get; set; }
    }
}
