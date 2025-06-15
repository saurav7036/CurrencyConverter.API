using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Models.Configurations.HttpHandler
{
    public record RetryPolicyOptions
    {
        public int RetryCount { get; set; }
        public int InitialDelaySeconds { get; set; }
    }
}
