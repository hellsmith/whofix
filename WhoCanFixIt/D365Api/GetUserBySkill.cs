using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;

namespace D365Api
{
    public static class GetUserBySkill
    {
        const string crmconnectionString = Skills.CrmConnectionString;
        [FunctionName("GetUserBySkill")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string skillname = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "skillname", true) == 0)
                .Value;


            if (skillname == null)
            {
                // Get request body
                dynamic data = await req.Content.ReadAsAsync<object>();
                skillname = data?.skillname;
            }

            var meineSkills = GetFromApi(skillname);
            var jsonString = JsonConvert.SerializeObject(meineSkills);

            return skillname == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, jsonString);
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

        public static IEnumerable<UserWithSkill> GetFromApi(string skillname)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            CrmServiceClient client = new CrmServiceClient(crmconnectionString);

            var crmService = client.OrganizationServiceProxy;

            var fetchXml = $@"
            <fetch>
              <entity name='bookableresource'>
                <attribute name='name' />
                <link-entity name='bookableresourcecharacteristic' from='resource' to='bookableresourceid' link-type='inner'>
                  <link-entity name='characteristic' from='characteristicid' to='characteristic' link-type='inner' alias='skill'>
                    <attribute name='name' />
                    <filter>
                      <condition attribute='name' operator='eq' value='{skillname}'/>
                    </filter>
                  </link-entity>
                  <link-entity name='ratingvalue' from='ratingvalueid' to='ratingvalue' link-type='inner' alias='level'>
                    <attribute name='value' />
                  </link-entity>
                </link-entity>
              </entity>
            </fetch>";

            var result = crmService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities.Select(x => new UserWithSkill()
            {
                Username = x.GetAttributeValue<string>("name"),
                Level = x.GetAttributeValue<int>("level"),
                skillname = x.GetAttributeValue<string>("skill")
            });


            return result;
        }
    }
}
