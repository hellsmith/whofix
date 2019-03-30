using System;
using System.Threading.Tasks;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using AdaptiveCards;
using System.Linq;
using SimpleEchoBot.Models;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class EchoDialog : IDialog<object>
    {

        public const string DYN_URL = "https://d365api20190330083214.azurewebsites.net/api/GetUserBySkill?code=Yas/x2o0YxaiW05Y2HXCLi0yhkicYfgKvMmfQHM/m3KzXesYd5JUAg==&skillname=";
        public const string LUIS_URL = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/a53891ff-21a9-4484-b9c5-bd624ea755c8?spellCheck=true&bing-spell-check-subscription-key=%7B4c880a82a88a481cb7fb555fba560250%7D&verbose=true&timezoneOffset=-360&subscription-key=c435e337eea04d12b113f4d30e394dea&q=";
        protected int count = 1;


        List<string> allTags = new List<string>() { "JavaScript", "C#", "SharePoint", "Dynamics" };

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            List<string> tags = new List<string>();

            if (!string.IsNullOrWhiteSpace(message.Text) && message.Attachments != null && message.Attachments.Count > 0)
            {
                await context.PostAsync("Please input either text or an image.");
                context.Wait(MessageReceivedAsync);
            }
            else
            {

                if (message.Attachments != null && message.Attachments.Count > 0)
                {
                    Attachment attachment = message.Attachments[0];
                    dynamic content = attachment.Content;
                    string url = content.downloadUrl;
                    var webClient = new WebClient();
                    byte[] imageBytes = webClient.DownloadData(url);


                    tags = GetTextRawData(attachment.ContentUrl);

                    context.ConversationData.SetValue<List<string>>("tags", tags);
                    context.ConversationData.SetValue<string>("image", attachment.ContentUrl);
                    context.ConversationData.SetValue<string>("textinput", "");

                    var replyMessage = context.MakeMessage();
                    Attachment cardAttachment = CreateTagChoiceAdapativecard(tags);
                    replyMessage.Attachments = new List<Attachment> { cardAttachment };

                    await context.PostAsync(replyMessage);
                }
                else if (!string.IsNullOrWhiteSpace(message.Text))
                {
                    tags = GetTextRawData(message.Text);

                    context.ConversationData.SetValue<List<string>>("tags", tags);
                    context.ConversationData.SetValue<string>("textinput", message.Text);
                    context.ConversationData.SetValue<string>("image", "");

                    var replyMessage = context.MakeMessage();
                    Attachment cardAttachment = CreateTagChoiceAdapativecard(tags);
                    replyMessage.Attachments = new List<Attachment> { cardAttachment };
                    

                    await context.PostAsync(replyMessage);


                }
                else if (message.Value != null)
                {
                    dynamic value = message.Value;

                    string skillsString = ((JObject)value).GetValue("MultiSelectVal").ToString();
                    string[] skills = skillsString.Split(',');

                    

                    List<Contact> contacts = GetDynamicsData(skills.ToList());

                    await context.PostAsync(string.Join(", ", contacts.Select(i => i.Username)));
                    context.Wait(MessageReceivedAsync);
                }
            }

        }

        private Attachment CreateTagChoiceAdapativecard(List<string> tags)
        {
            List<string> choices = new List<string>();
            foreach (string tag in tags)
            {
                choices.Add("{'title': '" + tag + "', 'value': '" + tag + "'}");
            }

            string json = @"{
                'type': 'AdaptiveCard',
                'body': [
                    {
                        'type': 'TextBlock',
                        'text': 'Which tag matches your query?'
                    },
                    {
                        'type': 'Input.ChoiceSet',
                        'id': 'MultiSelectVal',
                        'value': null,
                        'choices': [" +
                        string.Join(",", choices) +
                        @"],
                        'isMultiSelect': true
                    }
                ],
                'actions': [
                    {
                        'type': 'Action.Submit',
                        'title': 'Submit',
                        'data': {
                            'id': 'MultiSelectVal'
                        }
                    }
                ],
                '$schema': 'http://adaptivecards.io/schemas/adaptive-card.json',
                'version': '1.0'
            }";

            AdaptiveCard card = AdaptiveCard.FromJson(json).Card;

            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
            return attachment;
        }

        private Attachment CreateContactsCard(List<Contact> contacts)
        {
            List<string> choices = new List<string>();
            foreach (Contact contact in contacts)
            {
                choices.Add("{'title': '" + contact + "', 'value': '" + contact + "'}");
            }

            string json = @"{
                'type': 'AdaptiveCard',
                'body': [
                    {
                        'type': 'TextBlock',
                        'text': 'Which tag matches your query?'
                    },
                    {
                        'type': 'Input.ChoiceSet',
                        'id': 'MultiSelectVal',
                        'value': null,
                        'choices': [" +
                        string.Join(",", choices) +
                        @"],
                        'isMultiSelect': true
                    }
                ],
                'actions': [
                    {
                        'type': 'Action.Submit',
                        'title': 'Submit',
                        'data': {
                            'id': 'MultiSelectVal'
                        }
                    }
                ],
                '$schema': 'http://adaptivecards.io/schemas/adaptive-card.json',
                'version': '1.0'
            }";

            AdaptiveCard card = AdaptiveCard.FromJson(json).Card;

            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
            return attachment;
        }


        private List<Contact> GetDynamicsData(List<string> tags)
        {
            List<Contact> contacts = new List<Contact>();
            
            if (tags.Count > 0)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(DYN_URL + Uri.EscapeDataString(tags[0]));

                string result = "";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }

                JArray ob = (JArray)JsonConvert.DeserializeObject(result);

                foreach (dynamic entityObject in ob)
                {
                    contacts.Add(new Contact()
                    {
                        Username = entityObject.Username,
                        Level = entityObject.Level,
                        Skillname = entityObject.Skillname
                    });
                }
            }

            return contacts;
        }

        private List<string> GetTextRawData(string inputString)
        {
            List<string> entities = new List<string>();

            if (!string.IsNullOrWhiteSpace(inputString))
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(LUIS_URL + Uri.EscapeDataString(inputString));

                string result = "";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }

                dynamic ob = JsonConvert.DeserializeObject(result);
                var entitiesObject = ob?.entities;

                foreach (dynamic entityObject in ((JArray)entitiesObject))
                {
                    string entity = entityObject?.entity;
                    entities.Add(entity);
                }

            }
            return entities;
        }

        private void SendPositiveTextFeedback(List<string> tags, string textinput)
        {
            throw new NotImplementedException();
        }

        private void SendPositiveImageFeedback(List<string> tags, string imageUrl)
        {
            throw new NotImplementedException();
        }

        private void SendTextFeedback(List<string> tags, string textinput, bool positive)
        {
            throw new NotImplementedException();
        }

        private void SendImageFeedback(List<string> tags, string imageUrl, bool positive)
        {
            throw new NotImplementedException();
        }
    }


    
}