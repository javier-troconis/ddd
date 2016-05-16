using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
using shared;
using Xunit;

namespace tests
{
    public class SubmitApplicationTests
    {
        private WhenSubmittingApplicationState _state = new WhenSubmittingApplicationState();

        [Fact]
        public void when_application_has_not_been_started()
        {
            var state = _state;

            Assert.Throws<Exception>(() => SubmitApplication.Execute(state, "rich hickey"));
        }

        [Fact]
        public void when_application_is_submitted()
        {
            var state = _state.Handle(new ApplicationStarted());

            var actual = SubmitApplication.Execute(state, "rich hickey");

            var expected = new IEvent[] {new ApplicationSubmitted("rich hickey")};

            Assert.Equal(actual, expected);
        }

        [Fact]
        public void when_application_has_already_been_submitted()
        {
            var state = _state
                .Handle(new ApplicationStarted())
                .Handle(new ApplicationSubmitted("rich hickey"));

            Assert.Throws<Exception>(() => SubmitApplication.Execute(state, "rich hickey"));
        }
    }
}