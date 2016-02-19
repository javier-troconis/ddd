using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
	public abstract class Aggregate : IEventStream, IEquatable<IIdentity>
	{
		private readonly List<IEvent> _changes = new List<IEvent>();

		protected Aggregate(Guid id)
		{
			Id = id;
		}

		public Guid Id { get; }

		public int Version { get; private set; } = -1;

		protected void RecordThat<TEvent>(TEvent @event) where TEvent : Event<TEvent>
		{
			_changes.Add(@event);
			Apply(@event);
		}

		public IReadOnlyCollection<IEvent> Changes => _changes;

		public virtual void Apply(IEvent @event)
		{
			Version++;
		}

		public bool Equals(IIdentity other)
		{
			return other != null && other.GetType() == GetType() && other.Id == Id;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as IIdentity);
		}

		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
	}

	public abstract class Aggregate<TState> : Aggregate where TState : IEventSourcedEntity, new()
	{
		protected readonly TState State = new TState();

		protected Aggregate(Guid id) : base(id)
		{

		}

		public sealed override void Apply(IEvent @event)
		{
			@event.ApplyTo(State);
			base.Apply(@event);
		}
	}

}
