using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using static D365Api.GetUserBySkill;

namespace D365Api
{
    public static class GetData
    {
        public enum ThingType
        {
             Room = 7,
             Equipment = 4,
        };

        [FunctionName("GetData")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            const string crmConnectionString = "AuthType=Office365;Url=https://m365x338761.crm4.dynamics.com/;UserName=kl@M365x338761.onmicrosoft.com;Password=xxl1234!";
            log.Info("C# running ... :)");
            
            // Parse Paramter to Object (Params is an string aka json
           // List<MyPostData> paramList = await req.Content.ReadAsAsync<List<MyPostData>>();
            string json = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "data", true) == 0)
                    .Value;

            var paramList = JsonConvert.DeserializeObject<List<MyPostData>>(json);
            CrmServiceClient client = new CrmServiceClient(crmConnectionString);
            IOrganizationService crmService = client.OrganizationServiceProxy;
            List<UserWithSkill> responseList = new List<UserWithSkill>();

            foreach (var element in paramList)
            {
                switch (element.type.ToLower())
                {
                    case "room":
                        responseList.AddRange(GetByThing(crmService, element.name,ThingType.Room));
                        break;
                    case "skill":
                        responseList.AddRange(GetBySkill(crmService, element.name));
                        break;
                    case "equipment":
                        responseList.AddRange(GetByThing(crmService, element.name, ThingType.Equipment));
                        break;
                    default:
                        return req.CreateResponse(HttpStatusCode.BadRequest);
                        break;
                }
            }

            // Response
            var statusSucceded = HttpStatusCode.OK;
            var respJSON = responseList;
            return req.CreateResponse(statusSucceded, respJSON);
        }

        public class MyPostData
        {
            public string type { get; set; }
            public string name { get; set; }
        }

        public static List<UserWithSkill> GetBySkill(IOrganizationService crmService, string skillname)
        {
            var result = new List<string>();
            var users = new List<UserWithSkill>();

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
                <link-entity name='systemuser' from='systemuserid' to='userid' alias='user'>
                    <attribute name='internalemailaddress' />
                    <attribute name='fullname' />
                </link-entity>
             </entity>
            </fetch>";

            var crmResult = crmService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities;
            users.AddRange(
                crmResult.Select(x =>
                        new UserWithSkill(x.GetAliasedValue<string>("user.fullname"), x.GetAliasedValue<int>("level.value"), skillname, x.GetAliasedValue<string>("user.internalemailaddress"))
                    )
                );
            return users;


            //var fetchXml = $@"
            //<fetch>
            //  <entity name='bookableresource'>
            //    <attribute name='name' />
            //    <link-entity name='bookableresourcecharacteristic' from='resource' to='bookableresourceid' link-type='inner'>
            //      <link-entity name='characteristic' from='characteristicid' to='characteristic' link-type='inner' alias='skill'>
            //        <attribute name='name' />
            //        <filter>
            //          <condition attribute='name' operator='eq' value='{skillname}'/>
            //        </filter>
            //      </link-entity>
            //      <link-entity name='ratingvalue' from='ratingvalueid' to='ratingvalue' link-type='inner' alias='level'>
            //        <attribute name='value' />
            //      </link-entity>
            //    </link-entity>
            //    <link-entity name='systemuser' from='systemuserid' to='userid' alias='user'>
            //        <attribute name='internalemailaddress' />
            //    </link-entity>
            // </entity>
            //</fetch>";
            //return crmService.RetrieveMultiple(new FetchExpression(fetchXml));
        }

        
        public static List<UserWithSkill> GetByThing(IOrganizationService crmService, string room, ThingType tt)
        {
            var user = new List<UserWithSkill>();

            var fetchXml = $@"
            <fetch>
              <entity name='systemuser'>
                <attribute name='internalemailaddress' />
                <attribute name='fullname' />
                <link-entity name='bookableresource' from='new_responsibleperson' to='systemuserid' link-type='inner'>
                  <filter>
                    <condition attribute='name' operator='eq' value='{room}'/>
                    <condition attribute='resourcetype' operator='eq' value='{(int)tt}'/>
                  </filter>
                </link-entity>
              </entity>
            </fetch>";

            var crmResult = crmService.RetrieveMultiple(new FetchExpression(fetchXml)).Entities;
            user.AddRange(
                crmResult.Select(x =>
                    new UserWithSkill(x.GetAttributeValue<string>("fullname"), 0, room, x.GetAttributeValue<string>("internalemailaddress"))
                    )
                );
            return user;
        }

        //public static List<UserWithSkill> GetByEq(IOrganizationService crmService, string eq)
        //{
        //    var users = new List<UserWithSkill>();
        //    var result = new List<string>();

        //    var fetchXml = $@"
        //    <fetch>
        //      <entity name='bookableresource'>
        //        <link-entity name='bookableresourcecharacteristic' from='resource' to='bookableresourceid' link-type='inner'>
        //          <link-entity name='characteristic' from='characteristicid' to='characteristic' link-type='inner' alias='skill' />
        //          <link-entity name='bookableresource' from='bookableresourceid' to='resource' link-type='inner'>
        //            <attribute name='bookableresourceid' />
        //            <filter>
        //              <condition attribute='resourcetype' operator='eq' value='4' />
        //            </filter>
        //            <filter type='and'>
        //                <condition attribute='name' operator='eq' value='{eq}' />
        //            </filter>
        //          </link-entity>
        //        </link-entity>
        //      </entity>
        //    </fetch>";

        //    var tmpEntitys = crmService.RetrieveMultiple(new FetchExpression(fetchXml));
        //    List<Guid> ressources = new List<Guid>();

        //    fetchXml = @"
        //    <fetch>
        //        <entity name='bookableresource'>
        //        <link-entity name='bookableresourcecharacteristic' from='resource' to='bookableresourceid' link-type='inner'>
        //          <link-entity name='characteristic' from='characteristicid' to='characteristic' link-type='inner' alias='skill' />
        //          <link-entity name='bookableresource' from='bookableresourceid' to='resource' link-type='inner' alias='resource'>
        //            <attribute name='name' />
        //            <filter>
        //                <condition attribute='bookableresourceid' operator='in'>          
        //    ";
        //    foreach (var element in tmpEntitys.Entities)
        //    {
        //        fetchXml += "<value>" + element.Id.ToString() + "</value>";
        //    }
        //    fetchXml += $@"
        //                </condition>
        //            </filter>
        //          </link-entity>
        //        </link-entity>
        //      </entity>
        //    </fetch>
        //    ";

        //    return crmService.RetrieveMultiple(new FetchExpression(fetchXml));
        //}
    }
}

