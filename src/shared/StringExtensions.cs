using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace shared
{
    public static class StringExtensions
    {
		public static bool MatchesWildcard(this string value, string wildcard)
		{
			return !string.IsNullOrEmpty(value)
				&& new Regex($"^{Regex.Escape(wildcard).Replace("\\*", ".*")}$", RegexOptions.IgnoreCase).IsMatch(value);
		}
	}
}
