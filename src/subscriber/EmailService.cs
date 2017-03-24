using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace subscriber
{
	public delegate Task SendEmail(string from, string to);

    public class EmailService
	{
	    public Task SendEmail(string from, string to)
	    {
		    Console.WriteLine($"email sent from {from} to {to}");
		    return Task.Delay(2000);
	    }
    }
}
