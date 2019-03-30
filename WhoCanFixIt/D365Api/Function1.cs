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
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;

namespace D365Api
{
    public static class Skills
    {
        public const string CrmConnectionString = "AuthType=Office365;Url=https://m365x338761.crm4.dynamics.com/;UserName=kl@M365x338761.onmicrosoft.com;Password=xxl1234!";
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

            var meineSkills = GetSkills();

            return name == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, "Hello. Your Name:" + name + ". Die Benutzer sind: " + String.Join(", ", meineSkills));
        }

        public static IEnumerable<string> GetSkills()
        {
            var result = new List<string>();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            CrmServiceClient client = new CrmServiceClient(CrmConnectionString);

            var crmService = client.OrganizationServiceProxy;

            var fetchXml = $@"
            <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
              <entity name='characteristic'>
                <attribute name='name' />
                <attribute name='description' />
                <attribute name='characteristictype' />
                <attribute name='characteristicid' />
                <order attribute='name' descending='false' />
                <filter type='and'>
                  <condition attribute='characteristictype' operator='eq' value='1'/>
                </filter>
              </entity>
            </fetch>";
            return crmService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.Select(x => x.GetAttributeValue<string>("name"));
        }

        

        public class UserWithSkill
        {
            public string Username { get; set; }
            public int Level { get; set; }
            public string skillname { get; set; }

            public UserWithSkill(string username, int level, string skillname)
            {
                this.Username = username;
                this.Level = level;
                this.skillname = skillname;
            }

            public UserWithSkill()
            {
            }
        }
    }
}
