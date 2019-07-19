using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.DependencyInjection;

namespace azurefunction
{
	public class DependencyExtensionConfigProvider : IExtensionConfigProvider
	{
		private readonly IServiceProvider _serviceProvider;

		public DependencyExtensionConfigProvider(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		public void Initialize(ExtensionConfigContext context)
		{
			context
				.AddBindingRule<DependencyAttribute>()
				.BindToInput(x => _serviceProvider.GetRequiredService(x.Type));
		}

	}
}
