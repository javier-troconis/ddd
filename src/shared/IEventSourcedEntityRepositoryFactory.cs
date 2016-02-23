using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shared
{
	public interface IEventSourcedEntityRepositoryFactory
	{
		IEventSourcedEntityRepository CreateForStreamCategory(string streamCategory);
	}
}
