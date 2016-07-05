using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using contracts;
using shared;

namespace subscriber
{
    public class ApplicationStatusDocumentWriter : IMessageHandler<IApplicationStarted, Task>, IMessageHandler<IApplicationSubmitted, Task>
    {
        public Task Handle(IApplicationStarted message)
        {
            throw new NotImplementedException();
        }

        public Task Handle(IApplicationSubmitted message)
        {
            throw new NotImplementedException();
        }
    }
}
