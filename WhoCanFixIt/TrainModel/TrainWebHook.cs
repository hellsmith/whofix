using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace TrainModel
{
    public static class TrainWebHook
    {
        [FunctionName("TrainWebHook")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            var m = new TrainImpl(null);
            
            // parse query parameter
            var resp = req.CreateResponse(HttpStatusCode.OK, String.Join("",m.GetSkills()) );

            return resp;
        }
    }
}
