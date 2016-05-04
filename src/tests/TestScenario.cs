using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using shared;
using Xunit;

namespace tests
{
    public static class TestScenario
    {
        public static IWhen<TState> Given<TState>(TState state)
        {
            IGiven<TState> scenario = new TestScenarioWrapper<TState>();
            return scenario.Given(state);
        }

        private class TestScenarioWrapper<TState> : IGiven<TState>, IWhen<TState>, IThen
        {
            private Func<TState, IEnumerable<IEvent>> _action;
            private TState _state;
            
            public IWhen<TState> Given(TState state)
            {
                _state = state;
                return this;
            }

            public IThen When(Func<TState, IEnumerable<IEvent>> action)
            {
                _action = action;
                return this;
            }

            public bool Then(params IEvent[] expect)
            {
                var actual = _action(_state);
                return expect.SequenceEqual(actual);
            }
        }
    }

    

    public interface IGiven<TState>
    {
        IWhen<TState> Given(TState state);
    }

    public interface IWhen<out TState>
    {
        IThen When(Func<TState, IEnumerable<IEvent>> action);
    }

    public interface IThen
    {
        bool Then(params IEvent[] expect);
    }
}
