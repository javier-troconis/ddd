namespace shared
{
	// fold over recorded events
    public static class StateFolder
    {
        public static TState Fold<TState>(TState state, object @event)
        {
            return Fold(state, (dynamic)@event);
        }

        private static TState Fold<TState, TEvent>(TState state, TEvent @event)
        {
            var handler = state as IMessageHandler<TEvent, TState>;
            return Equals(handler, null) ? state : handler.Handle(@event);
        }
    }
}
