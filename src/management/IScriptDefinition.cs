using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace management
{
    public interface IScriptDefinition<in TScriptData>
    {
	    string Type { get; }
	    IReadOnlyList<Func<TScriptData, object>> Activities { get; }
    }
}
