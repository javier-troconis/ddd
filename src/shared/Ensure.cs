using System;

namespace shared
{
    public static class Ensure
    {
		public static T NotDefault<T>(T argument, string argumentName)
		{
			if (Equals(default(T), argument))
			{
				throw new ArgumentException(argumentName);
			}
            return argument;
		}
	}
}
