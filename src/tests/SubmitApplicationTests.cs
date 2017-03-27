using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
using shared;
using Xunit;

namespace tests
{
    //public class SubmitApplicationTests
    //{
    //    private WhenSubmittingApplicationState _state = new WhenSubmittingApplicationState();

    //    [Fact]
    //    public void when_application_has_not_been_started()
    //    {
    //        var state = _state;

    //        Assert.Throws<Exception>(() => SubmitApplication.Apply(state, "rich hickey"));
    //    }

    //    [Fact]
    //    public void when_application_is_submitted()
    //    {
    //        var state = _state.Handle(new ApplicationStartedV1());

    //        var actual = SubmitApplication.Apply(state, "rich hickey");

    //        var expected = new IEvent[] {new ApplicationSubmittedV1("rich hickey")};

    //        Assert.Equal(actual, expected);
    //    }

    //    [Fact]
    //    public void when_application_has_already_been_submitted()
    //    {
    //        var state = _state
    //            .Handle(new ApplicationStartedV1())
    //            .Handle(new ApplicationSubmittedV1("rich hickey"));

    //        Assert.Throws<Exception>(() => SubmitApplication.Apply(state, "rich hickey"));
    //    }
    //}
}