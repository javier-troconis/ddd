using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
	public static class TypeExtensions
	{
		public static Type[] GetMessageHandlerTypes(this Type subscriberType)
		{
			return subscriberType
				.GetInterfaces()
				.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IMessageHandler<,>))
				.ToArray();
		}
	}
}
