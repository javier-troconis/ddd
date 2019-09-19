using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using shared;

namespace idology.api.messaging.host
{
    public class XVerifyIdentityRequest
    {
        public string Ssn { get; set; }
    }

    public enum XDecision
    {
        Passed,
        Failed
    }

    public class XVerifyIdentityResponse
    {
        public string TransactionId { get; set; }
        public XDecision Decision { get; set; }
    }

    public class XVerifyIdentityService : IMessageHandler<XVerifyIdentityRequest, Task<XVerifyIdentityResponse>>
    {
        public Task<XVerifyIdentityResponse> Handle(XVerifyIdentityRequest message)
        {       
            var response = new XVerifyIdentityResponse
            {
                TransactionId = message.Ssn,
                Decision = XDecision.Passed
            };
            return Task.FromResult(response);
        }
    }
}
