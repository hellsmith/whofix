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
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        private const string trainingKey = "12faac6f180a4e1e9053133826b3f188";
        private const string predictionKey = "91fa8c7baf2347b09b05e3c05254bc27";
        private const string resourceId = "/subscriptions/9981a4ee-be32-4cf7-939a-9e13ab373b8f/resourceGroups/rg_WhoCanFixIt/providers/Microsoft.CognitiveServices/accounts/fixit-vision-api-key";
        private static Guid PROJECT_ID = new Guid("743035e8-4a5a-4f6e-ae4e-97b9e8b95f81");
        private const string endpointUrl = "https://westeurope.api.cognitive.microsoft.com";
        private const string PUBLISHED_MODEL_NAME = "Iteration 1";

        public const string LUIS_URL = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/a53891ff-21a9-4484-b9c5-bd624ea755c8?spellCheck=true&bing-spell-check-subscription-key=%7B4c880a82a88a481cb7fb555fba560250%7D&verbose=true&timezoneOffset=-360&subscription-key=c435e337eea04d12b113f4d30e394dea&q=";
        protected int count = 1;

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            List<string> inputUrls = new List<string>();
            string inputString = "";

            if (message.Attachments != null && message.Attachments.Count > 0)
            {
                foreach (Attachment attachment in message.Attachments)
                {
                    inputUrls.Add(attachment.ContentUrl);
                }
            }
            if (!string.IsNullOrWhiteSpace(message.Text))
            {
                inputString = message.Text;
            }

            List<string> textEntities =  GetTextRawData(inputString);

            //get data that matches the inputs

            PromptDialog.Confirm(
                context,
                AfterResetAsync,
                "that's what I got: " +  String.Join(", ", textEntities),
                promptStyle: PromptStyle.Auto
                );

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

        //private T parseMyShit(JObject jObject, string property)
        //{
        //    JToken token = null;
        //    if (jObject.TryGetValue(property, out token))
        //    {
        //        return (T)token.va;
        //    }

        //}

        public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
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
        
        public void AddImage(List<Tag> tags,byte[] imgBytes)
        {
            // Create the Api, passing in the training key
            CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient()
            {
                ApiKey = trainingKey,
                Endpoint = endpointUrl
            };

            var project = trainingApi.GetProject(PROJECT_ID);

            List<Guid> tagIds = new List<Guid>();

            foreach(var tag in tags)
            {
                tagIds.Add(tag.Id);
            }

            // Images can be uploaded one at a time
            using (var stream = new MemoryStream(imgBytes))
            {
                trainingApi.CreateImagesFromData(project.Id, stream, tagIds);
            }
        }

        public IList<PredictionModel> CheckImage(byte [] imgBytes)
        {
            // Create a prediction endpoint, passing in obtained prediction key
            CustomVisionPredictionClient endpoint = new CustomVisionPredictionClient()
            {
                ApiKey = predictionKey,
                Endpoint = endpointUrl
            };

            using (MemoryStream mem = new MemoryStream(imgBytes))
            {
                // Make a prediction against the new project
                var result = endpoint.DetectImage(PROJECT_ID, PUBLISHED_MODEL_NAME,mem);
                
                return result.Predictions;
            }
        }
    }
}