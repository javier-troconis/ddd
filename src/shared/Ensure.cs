using System;

namespace shared
{
    public static class Ensure
    {
		public static void NotDefault<T>(T argument, string argumentName)
		{
			if (Equals(argument, default(T)))
			{
				throw new ArgumentException(argumentName);
			}
		}

        public static void NotNull<T>(T argument, string argumentName) where T : class
        {
            if (Equals(argument, null))
            {
                throw new ArgumentNullException(argumentName);
            }
        }
    }
}
