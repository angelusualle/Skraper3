using System.Net.Http;
using System.Threading.Tasks;

namespace Skraper3
{
    internal class Subscription
    {
        public string Email {get;set;}
        public string MobileNumber{get;set;}
        public string URL{get;set;}

        public bool Changed {get;set;}
        public Task<HttpResponseMessage> response {get;set;}
    }
}