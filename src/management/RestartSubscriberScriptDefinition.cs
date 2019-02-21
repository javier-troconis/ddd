using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eventstore;

namespace management
{
	public class RestartSubscriberScriptData
	{
		public string SubscriberName { get; set; }
	}

	public class RestartSubscriberScriptDefinition : IScriptDefinition<RestartSubscriberScriptData>
	{
		public string ScriptType => "RestartSubscriberScript";

		public IReadOnlyList<ScriptActivity<RestartSubscriberScriptData>> Activities =>
			new[]
			{
				new ScriptActivity<RestartSubscriberScriptData>(Guid.Parse("E3E65DE6-630E-4793-91FC-21DECE115491"),
					nameof(StopSubscriber), scriptData => new StopSubscriber(scriptData.SubscriberName)),

				new ScriptActivity<RestartSubscriberScriptData>(Guid.Parse("42821354-E8BF-4FAA-A38F-A7940B59B882"),
					nameof(StartSubscriber), scriptData => new StartSubscriber(scriptData.SubscriberName)),

				new ScriptActivity<RestartSubscriberScriptData>(Guid.Parse("D7257AF7-7910-4E66-B14B-D691232D7B30"),
					nameof(StopSubscriber), scriptData => new StopSubscriber(scriptData.SubscriberName)),

				new ScriptActivity<RestartSubscriberScriptData>(Guid.Parse("F4FC788F-2498-4667-9723-532111930F9E"),
					nameof(StartSubscriber), scriptData => new StartSubscriber(scriptData.SubscriberName))
			};
	}
}
