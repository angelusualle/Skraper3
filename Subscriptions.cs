using System;
using System.Collections.Generic;

namespace Skraper3
{
    public partial class Subscriptions
    {
        public string Email { get; set; }
        public string Url { get; set; }
        public string MobileNumber { get; set; }
        public string Xpath { get; set; }
        public long? NumberOfErrors { get; set; }
        public long? Changed { get; set; }
    }
}
