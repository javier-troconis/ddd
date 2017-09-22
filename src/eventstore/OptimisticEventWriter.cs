//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;

//using EventStore.ClientAPI;
//using EventStore.ClientAPI.Exceptions;

//namespace eventstore
//{
//	public delegate bool TryResolveConflict(IEnumerable<object> newEvents, IEnumerable<object> conflictingEvents, out IEnumerable<object> mergedEvents);

//	public static class ConflictResolutionStrategy
//	{
//		public static readonly TryResolveConflict IgnoreConflicts = delegate(IEnumerable<object> newEvents, IEnumerable<object> conflictingEvents, out IEnumerable<object> mergedEvents)
//		{
//			mergedEvents = newEvents;
//			return true;
//		};
//	}

//	public static class OptimisticEventWriter
//	{
//		public static async Task<WriteResult> WriteEvents(
//			IEventStore eventStore,
//			string streamName,
//			long streamExpectedVersion,
//			IEnumerable<object> events,
//			TryResolveConflict tryResolveConflict,
//			Func<EventDataSettings, EventDataSettings> configureEventDataSettings = null)
//		{
//			try
//			{
//				return await eventStore.WriteEvents(streamName, streamExpectedVersion, events, configureEventDataSettings);
//			}
//			catch (WrongExpectedVersionException ex)
//			{
//				streamExpectedVersion = long.Parse(Regex.Match(ex.Message, @"Current version: ((-|)\d+)$").Groups[1].Value);
//			}
//			if (streamExpectedVersion == ExpectedVersion.NoStream)
//			{
//				return await WriteEvents(eventStore, streamName, streamExpectedVersion, events, tryResolveConflict, configureEventDataSettings);
//			}
//			IEnumerable<object> conflictingEvents;
//			do
//			{
//				conflictingEvents = await eventStore.ReadEventsForward(streamName, streamExpectedVersion);
//			} while (!conflictingEvents.Any());
//			IEnumerable<object> mergedEvents;
//			if (!tryResolveConflict(events, conflictingEvents, out mergedEvents))
//			{
//				throw new StreamConcurrencyException();
//			}
//			return await WriteEvents(eventStore, streamName, streamExpectedVersion, mergedEvents, tryResolveConflict, configureEventDataSettings);
//		}
//	}
//}
