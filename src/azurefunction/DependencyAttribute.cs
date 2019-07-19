using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs.Description;

namespace azurefunction
{
	[Binding]
	[AttributeUsage(AttributeTargets.Parameter)]
	public class DependencyAttribute : Attribute
	{
		public DependencyAttribute(Type type)
		{
			Type = type;
		}
		public Type Type { get; }
	}
}
