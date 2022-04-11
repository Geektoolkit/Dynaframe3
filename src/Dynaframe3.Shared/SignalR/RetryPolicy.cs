using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynaframe3.Shared.SignalR
{
    public class RetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            // Ensure we keep trying forever with a backoff up to 30 seconds.
            var retryCount = retryContext.PreviousRetryCount + 1;
            return TimeSpan.FromSeconds(Math.Min(retryCount * 5, 30));
        }
    }
}
