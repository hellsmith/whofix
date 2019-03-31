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
using System.Net.Http.Headers;
using System.Web;
using SimpleEchoBot.Models;
using System.Globalization;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        private const string VIS_COG_URL = "http://whocanfixitapp.azurewebsites.net";
        private const string VIS_COG_CHECK = "/CheckImage";
        private const string VIS_COG_ADD = "/AddImage";

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

            List<Tag> tags = new List<Tag>();

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

                    //string base64String = Convert.ToBase64String(imageBytes);

                    List<TagPrediction> predictions = await GetImageRawData(imageBytes);
                    tags = predictions.Select(i => new Tag() { ID = i.TagId, Name = i.TagName, Type = i.TagDesc }).ToList();

                    Dictionary<string, List<Tag>> multipleTags = FindMultiples(predictions);
                    foreach(var entry in multipleTags)
                    {
                        foreach(var tag in entry.Value)
                        {
                            predictions = predictions.Where(k => k.TagId != tag.ID).ToList();
                        }
                    }

                    context.ConversationData.SetValue<List<Tag>>("tags", tags);
                    context.ConversationData.SetValue<Dictionary<string, List<Tag>>>("multiples", multipleTags);
                    context.ConversationData.SetValue<string>("image", attachment.ContentUrl);
                    context.ConversationData.SetValue<string>("textinput", "");
                    
                    var replyMessage = context.MakeMessage();
                    Attachment cardAttachment = CreateTagChoiceResponse(multipleTags);
                    replyMessage.Attachments = new List<Attachment> { cardAttachment };

                    await context.PostAsync(replyMessage);
                }
                else if (!string.IsNullOrWhiteSpace(message.Text))
                {
                    tags = GetTextRawData(message.Text);

                    context.ConversationData.SetValue<List<Tag>>("tags", tags);
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

                    string dialogType = ((JObject)value).GetValue("id").ToString();

                    if (dialogType == "MultiSelect")
                    {

                        string skillsString = ((JObject)value).GetValue("MultiSelectVal").ToString();
                        string[] skills = skillsString.Split(',');


                        List<Contact> contacts = GetDynamicsData(skills.ToList());

                        if (contacts.Count > 0)
                        {

                            var replyMessage = context.MakeMessage();
                            Attachment contactAttachment = CreateContactsCard(contacts);
                            replyMessage.Attachments = new List<Attachment> { contactAttachment };

                            await context.PostAsync(replyMessage);
                        }
                        else
                        {
                            await context.PostAsync("Sorry, I could not find any people with this skill");
                            context.Wait(MessageReceivedAsync);
                        }
                    }
                    else if (dialogType == "MultiMultiSelect")
                    {
                        tags = context.ConversationData.GetValue<List<Tag>>("tags");
                        Dictionary<string, List<Tag>> multipleTags=  context.ConversationData.GetValue<Dictionary<string, List<Tag>>>("multiples");

                        List<Tag> sendList = new List<Tag>();
                        
                    }
                }
            }

        }

        private Attachment CreateTagChoiceAdapativecard(List<Tag> tags)
        {

            List<string> choices = new List<string>();
            foreach (Tag tag in tags)
            {
                choices.Add("{'title': '" + tag.Name + "', 'value': '" + tag.Name + "'}");
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
                            'id': 'MultiSelect'
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
                choices.Add(@"{
                        'type': 'FactSet', 
                        'facts': [
                            {
                                'title': 'Name', 
                                'value': '" + contact.Username + @"'
                            },
                            {
                                'title': 'Email', 
                                'value': '" + contact.Email + @"'
                            },                    
                            {
                                'title': 'Skill', 
                                'value': '" + contact.Skillname + @"'
                            },
                            {
                                'title': 'Level', 
                                'value': '" + contact.Level + @"'
                            },
                        ]}");
            }

            string json = @"{ 
                'type': 'AdaptiveCard', 
                'body': [ 
                    { 
                        'type': 'TextBlock', 
                        'size': 'Medium', 
                        'weight': 'Bolder', 
                        'text': 'Contacts' 
                    }," +
                    string.Join(",", choices) + @"],
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


        private async Task<List<TagPrediction>> GetImageRawData(byte[] rawData)
        {
            List<TagPrediction> results = new List<TagPrediction>();

            try
            {
                results = await LoadPredictions(rawData);

            }
            catch (Exception e)
            {
                throw;
            }
            return results;
        }

        private static async Task<string> sendBase64Image(string base64string, HttpClient client)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync(
                            VIS_COG_CHECK.TrimStart('/'), base64string);
            return await response.Content.ReadAsAsync<string>();
        }

        private List<Tag> GetTextRawData(string inputString)
        {
            List<Tag> entities = new List<Tag>();

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
                    entities.Add(new Tag() { Name = entity, Type = "Skill" });
                }

            }
            return entities;
        }

        private List<TagPrediction> GetImageStuff(string url)
        {
            List<TagPrediction> entities = new List<TagPrediction>();

            if (!string.IsNullOrWhiteSpace(url))
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(VIS_COG_URL + "/checkimageurl?img=" + Uri.EscapeUriString(url));

                string result = "";

                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        result = reader.ReadToEnd();
                    }

                    entities = JsonConvert.DeserializeObject<List<TagPrediction>>(result);
                }
                catch (Exception e)
                {


                }

            }
            return entities;
        }

        private Attachment CreateTagChoiceResponse(Dictionary<string, List<Tag>> tags)
        {
            List<string> choiceIds = new List<string>();
            List<string> choiceList = new List<string>();
            foreach (var entry in tags)
            {
                List<string> choices = new List<string>();
                choiceIds.Add("MultiSelect" + entry.Key);
                choices.Add(@"{
                        'type': 'TextBlock',
                        'text': 'Which " + entry.Key + @"?'
                    },
                    {
                        'type': 'Input.ChoiceSet',
                        'id': 'MultiSelect" + entry.Key + @"',
                        'value': null,
                        'choices': ["
                        );

                foreach (var tag in entry.Value)
                {
                    choices.Add("{'title': '" + tag.Name + "', 'value': '" + tag.ID + "'}");
                }

                choices.Add("{'title': 'Keines', 'value': 'none'}");

                choiceList.Add(string.Join(",", choices));

                choices.Add(@"],
                        'isMultiSelect': true
                    }");
            }

            string json = @"{
                'type': 'AdaptiveCard',
                'body': ["
                    + string.Join(",", choiceList) +
                @"],
                'actions': [
                    {
                        'type': 'Action.Submit',
                        'title': 'Submit',
                        'data': {
                            'id': 'MultiMultiSelect'
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

        public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var positive = await argument;

            List<string> tags = context.ConversationData.GetValueOrDefault<List<string>>("tags", new List<string>());
            string imageUrl = context.ConversationData.GetValueOrDefault<string>("image", "");
            string textinput = context.ConversationData.GetValueOrDefault<string>("textinput", "");

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                SendPositiveImageFeedback(tags, imageUrl);
            }
            else if (!string.IsNullOrWhiteSpace(textinput))
            {
                SendPositiveTextFeedback(tags, textinput);
            }

            if (positive)
            {
                //user was satisfied with the result
                await context.PostAsync("Thanks for your feedback!");
            }
            else
            {
                //user was not satisfied
                await context.PostAsync("I'll do better next time!");
            }
            context.Wait(MessageReceivedAsync);
        }

        public async Task<List<TagPrediction>> LoadPredictions(byte[] image)
        {
            using (var client = new HttpClient())
            {
                using (var content = new MultipartFormDataContent())
                {
                    content.Add(new StringContent(Convert.ToBase64String(image)), "data");

                    using (var message = await client.PostAsync(VIS_COG_URL + VIS_COG_CHECK, content))
                    {
                        var input = await message.Content.ReadAsStringAsync();

                        //JsonConvert.DeserializeObject<List<TagPrediction>>(input);

                        JArray ob = (JArray)JsonConvert.DeserializeObject(input);
                        List<TagPrediction> preds = new List<TagPrediction>();
                        foreach (dynamic e in ob)
                        {
                            try
                            {
                                preds.Add(new TagPrediction()
                                {
                                    TagDesc = e.TagDesc,
                                    TagId = e.TagId,
                                    TagName = e.TagName,
                                    TagProbability = e.TagProbability
                                });
                            }
                            catch (Exception)
                            {
                            }
                        }
                        return preds;
                    }
                }
            }
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

        public Dictionary<string, List<Tag>> FindMultiples(List<TagPrediction> predictions)
        {
            Dictionary<string, List<Tag>> result = new Dictionary<string, List<Tag>>();
            Dictionary<string, int> counts = new Dictionary<string, int>();

            foreach (var pred in predictions)
            {
                if (pred.TagProbability > 0.5f)
                {
                    pred.TagDesc = string.IsNullOrEmpty(pred.TagDesc) ? "Skill" : pred.TagDesc; 
                    if (counts.ContainsKey(pred.TagDesc))
                    {
                        counts[pred.TagDesc]++;
                    }
                    else
                    {
                        counts.Add(pred.TagDesc, 1);
                    }
                }
            }

            foreach (var pred in predictions)
            {
                if (!string.IsNullOrEmpty(pred.TagDesc))
                {
                    if (counts[pred.TagDesc] > 1)
                    {
                        if (result.ContainsKey(pred.TagDesc))
                        {
                            result[pred.TagDesc].Add(new Tag()
                            {
                                ID = pred.TagId,
                                Name = pred.TagName,
                                Type = pred.TagDesc
                            });
                        }
                        else
                        {
                            result.Add(pred.TagDesc, new List<Tag>(){new Tag()
                            {
                                ID = pred.TagId,
                                Name = pred.TagName,
                                Type = pred.TagDesc
                            } });
                        }
                    }
                }
            }

            return result;
        }
    }
}