using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using aXon.Dynamics.Client;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Tooling.Connector;

namespace TrainModel
{
    public class TrainImpl
    {
        Uri endpoint = null;
        const string crmconnectionString = "AuthType=Office365;Url=https://m365x338761.crm4.dynamics.com/;UserName=ta@M365x338761.onmicrosoft.com;Password=xxl1234!";
        public TrainImpl(Uri endpoint)
        {
            this.endpoint = endpoint;
        }

        public void Train (IEnumerable<string> skills)
        {

        }

        public List<string> GetSkills()
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
