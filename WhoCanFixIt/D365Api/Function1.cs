using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Tooling.Connector;

namespace D365Api
{
    public static class Skills
    {
        const string crmconnectionString = "AuthType=Office365;Url=https://m365x338761.crm4.dynamics.com/;UserName=ta@M365x338761.onmicrosoft.com;Password=xxl1234!";

        [FunctionName("GetSkills")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {         
           log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string name = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
                .Value;


            if (name == null)
            {
                // Get request body
                dynamic data = await req.Content.ReadAsAsync<object>();
                name = data?.name;
            }

            var meinBenutzer = GetSkills();

            return name == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, "Hello. Your Name:" + name + ". Die Benutzer sind: " + String.Join(", ", meinBenutzer));
        }
        public static List<string> GetSkills()
        {
            var result = new List<string>();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            CrmServiceClient client = new CrmServiceClient(crmconnectionString);

            var crmService = client.OrganizationServiceProxy;

            WhoAmIRequest who = new WhoAmIRequest();
            WhoAmIResponse whoResp = (WhoAmIResponse)crmService.Execute(who);
            result.Add(whoResp.UserId.ToString());
            return result;
        }
    }
}
