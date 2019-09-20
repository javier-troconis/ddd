using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using shared;

namespace idology.api.messaging.host
{
    public class VerifyIdentityServiceByProviderName : ReadOnlyDictionary<string, IMessageHandler>
    {
        public static readonly VerifyIdentityServiceByProviderName Value = new VerifyIdentityServiceByProviderName();

        private VerifyIdentityServiceByProviderName() : base(
            new Dictionary<string, IMessageHandler>
        {
            ["echo"] = new MessageHandlerResultToEventService<object>("verifyidentity", 
                new EchoService().ComposeForward(new RecordToMessageService("verifyidentitysucceeded"))),
            ["x"] = new MessageHandlerResultToEventService<XVerifyIdentityRequest>("verifyidentity", 
                new XVerifyIdentityService().ComposeForward(new XVerifyResponseToEventService()))
        })
        {

        }
    }
}
