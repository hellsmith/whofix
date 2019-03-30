using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Linq;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Teams.Samples.HelloWorld.Web.Models;
using Newtonsoft.Json;

namespace Microsoft.Teams.Samples.HelloWorld.Web.Controllers
{
    public class HomeController : Controller
    {
        private const string trainingKey = "12faac6f180a4e1e9053133826b3f188";
        private const string predictionKey = "91fa8c7baf2347b09b05e3c05254bc27";
        private const string resourceId = "/subscriptions/9981a4ee-be32-4cf7-939a-9e13ab373b8f/resourceGroups/rg_WhoCanFixIt/providers/Microsoft.CognitiveServices/accounts/fixit-vision-api-key";
        private static Guid PROJECT_ID = new Guid("743035e8-4a5a-4f6e-ae4e-97b9e8b95f81");
        private const string endpointUrl = "https://westeurope.api.cognitive.microsoft.com";
        private const string trainingEndPointUrl = "Training/";
        private const string predictionEndPointUrl = "Prediction/";
        private const string PUBLISHED_MODEL_NAME = "Iteration2";

        private static MemoryStream testImage;

        [Route("")]
        public ActionResult Index()
        {
            return View();
        }

        [Route("hello")]
        public ActionResult Hello()
        {
            return View("Index");
        }

        [Route("first")]
        public ActionResult First()
        {
            return View();
        }

        [Route("second")]
        public ActionResult Second()
        {
            return View();
        }

        [Route("configure")]
        public ActionResult Configure()
        {
            return View();
        }

        [Route("predict")]
        public ActionResult Predict()
        {
            try
            {
                ViewBag.Prediction = MakePredictionRequest("http://www.saxen.co.uk/pub/media/catalog/product/cache/2845706bd23548ef25c28effb607a85b/g/r/grange-corner-desk-printer-cpu-pedestal_1.jpg").Result;
            }catch(Exception ex)
            {
                ViewBag.ErrorMsg = ex.Message;
                ViewBag.ErrorStack = ex.StackTrace;
            }
            return View();
        }

        [Route("TagUpdate"),HttpPost]
        public ActionResult TagUpdate(string tagid,string type, string description)
        {
            if (!string.IsNullOrEmpty(tagid))
            {
                // Create the Api, passing in the training key
                CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient()
                {
                    ApiKey = trainingKey,
                    //Endpoint = endpointUrl + trainingEndPointUrl
                    Endpoint = endpointUrl
                };

                var project = trainingApi.GetProject(PROJECT_ID);

                Tag tag = trainingApi.GetTag(project.Id, new Guid(tagid));

                tag.Type = type;
                tag.Description = description;

                trainingApi.UpdateTag(project.Id, tag.Id, tag);
            }

            return View();
        }

        [Route("tagedit")]
        public ActionResult TagEdit()
        {
            string tagName = Request["tagName"];

            if (!string.IsNullOrEmpty(tagName))
            {
                // Create the Api, passing in the training key
                CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient()
                {
                    ApiKey = trainingKey,
                    //Endpoint = endpointUrl + trainingEndPointUrl,
                    Endpoint = endpointUrl
                };
                
                IList<Tag> tags = trainingApi.GetTags(PROJECT_ID);

                Tag tag = tags.Where(t => t.Name == tagName).First();
                if (tag != null)
                {
                    ViewBag.TagType = tag.Type;
                    ViewBag.TagDescr = tag.Description;
                    ViewBag.TagId = tag.Id;
                }
                else
                {
                    ViewBag.TagType = "tagName not found";
                    ViewBag.TagDescr = "-/-";
                    ViewBag.TagId = "-/-";
                }
            }
            else
            {
                ViewBag.TagType = "tagName parameter not set";
                ViewBag.TagDescr = "-/-";
                ViewBag.TagId = "-/-";
            }
            return View();
        }

        [Route("checkimage")]
        public ActionResult CheckImage(byte[] data)
        {
            //byte[] img = null;

            

            //if (!string.IsNullOrEmpty(img))
            //{
            //    data = data.Substring(5);
            //    img = Convert.FromBase64String(data);
            //}

            if (data != null && data.Length > 0)
            {
                return Json(CheckImageData(data), JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new List<TagPrediction>()
                {
                    new TagPrediction()
                    {
                        TagDesc = "no description",
                        TagId = new Guid(),
                        TagName = "no name",
                        TagProbability = 1
                    }
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [Route("checkimageurl")]
        public ActionResult CheckImageUrl()
        {
            string imgUrl = Request["img"];
            if (!string.IsNullOrEmpty(imgUrl))
            {
                var webClient = new WebClient();
                webClient.Credentials = new NetworkCredential("bb@M365x338761.onmicrosoft.com", "xxl1234!");
                byte[] imageBytes = webClient.DownloadData(imgUrl);

                return Json(CheckImageData(imageBytes), JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new List<TagPrediction>()
                {
                    new TagPrediction()
                    {
                        TagDesc = "no description",
                        TagId = new Guid(),
                        TagName = "no name",
                        TagProbability = 1
                    }
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [Route("addimage")]
        public void AddImage(string data)
        {
            TagResponse resp = JsonConvert.DeserializeObject<TagResponse>(data);

            // Create the Api, passing in the training key
            CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient()
            {
                ApiKey = trainingKey,
                Endpoint = endpointUrl
            };

            var project = trainingApi.GetProject(PROJECT_ID);

            byte[] imgBytes = Convert.FromBase64String(resp.ImgBase64);

            foreach(string tagName in resp.TagNames)
            {
                var tag = trainingApi.CreateTag(PROJECT_ID, tagName);
                resp.TagIds.Add(tag.Id);
            }

            // Images can be uploaded one at a time
            using (var stream = new MemoryStream(imgBytes))
            {
                trainingApi.CreateImagesFromData(project.Id, stream, resp.TagIds);
            }
        }

        protected List<TagPrediction> CheckImageData(byte[] imgBytes)
        {
            // Create a prediction endpoint, passing in obtained prediction key
            CustomVisionPredictionClient endpoint = new CustomVisionPredictionClient()
            {
                ApiKey = predictionKey,
                Endpoint = endpointUrl
            };

            CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient()
            {
                ApiKey = trainingKey,
                Endpoint = endpointUrl
            };

            var allTags = trainingApi.GetTags(PROJECT_ID);

            using (MemoryStream mem = new MemoryStream(imgBytes))
            {
                // Make a prediction against the new project
                var result = endpoint.ClassifyImage(PROJECT_ID, PUBLISHED_MODEL_NAME, mem);

                List<TagPrediction> predicts = new List<TagPrediction>();
                
                foreach (var entry in result.Predictions)
                {
                    Tag tag = allTags.Where(k => k.Id == entry.TagId).First();
                    predicts.Add(new TagPrediction()
                    {
                        TagDesc = tag.Description,
                        TagId = entry.TagId,
                        TagName = entry.TagName,
                        TagProbability = entry.Probability
                    });
                }

                return predicts;
            }
        }

        public static async Task<string> MakePredictionRequest(string imageFilePath)
        {
            var client = new HttpClient();

            // Request headers - replace this example key with your valid Prediction-Key.
            client.DefaultRequestHeaders.Add("Prediction-Key", "588e20e261ca4a3aa631eed683e93edc");
            // Prediction URL - replace this example URL with your valid Prediction URL.
            string url = "https://westeurope.api.cognitive.microsoft.com/vision/v2.0/analyze"; //"https://southcentralus.api.cognitive.microsoft.com/customvision/v3.0/Prediction/8622c779-471c-4b6e-842c-67a11deffd7b/classify/iterations/Cats%20vs.%20Dogs%20-%20Published%20Iteration%203/image";

            HttpResponseMessage response;
            // Request body. Try this sample with a locally stored image.

            byte[] byteData = GetImageByUrl(imageFilePath);
            
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(url, content);
                //Console.WriteLine(await response.Content.ReadAsStringAsync());
                return await response.Content.ReadAsStringAsync();
            }
        }

        private static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }

        public static byte [] GetImageByUrl(string url)
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadData(new Uri(url));
            }
        }

        public static IList<Tag> GetTags()
        {
            // Create the Api, passing in the training key
            CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient()
            {
                ApiKey = trainingKey,
                Endpoint = endpointUrl + trainingEndPointUrl
            };

            var project = trainingApi.GetProject(PROJECT_ID);

            return trainingApi.GetTags(project.Id);
        }

        public static void AddImage(List<Guid> tags,byte [] imgBytes)
        {

            // Create the Api, passing in the training key
            CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient()
            {
                ApiKey = trainingKey,
                Endpoint = endpointUrl + trainingEndPointUrl
            };

            var project = trainingApi.GetProject(PROJECT_ID);

            // Create a new project
            //var project = trainingApi.CreateProject("My New Project");

            // Make two tags in the new project
            //var hemlockTag = trainingApi.CreateTag(project.Id, "Hemlock");
            //var japaneseCherryTag = trainingApi.CreateTag(project.Id, "Japanese Cherry");
            

            // Images can be uploaded one at a time
            using (var stream = new MemoryStream(imgBytes))
            {
                trainingApi.CreateImagesFromData(project.Id, stream, tags);
            }
            
            //// Now there are images with tags start training the project
            //var iteration = trainingApi.TrainProject(project.Id);

            //// The returned iteration will be in progress, and can be queried periodically to see when it has completed
            //while (iteration.Status == "Training")
            //{
            //    Thread.Sleep(1000);

            //    // Re-query the iteration to get it's updated status
            //    iteration = trainingApi.GetIteration(project.Id, iteration.Id);
            //}

            //// The iteration is now trained. Make it the default project endpoint
            //iteration.IsDefault = true;
            //trainingApi.UpdateIteration(project.Id, iteration.Id, iteration);

            // Now there is a trained endpoint, it can be used to make a prediction

            //// Create a prediction endpoint, passing in obtained prediction key
            //CustomVisionPredictionClient endpoint = new CustomVisionPredictionClient()
            //{
            //    ApiKey = predictionKey,
            //    Endpoint = SouthCentralUsEndpoint
            //};

            //// Make a prediction against the new project
            //Console.WriteLine("Making a prediction:");
            //var result = endpoint.PredictImage(project.Id, testImage);

            //List<string> probabilities = new List<string>();

            //// Loop over each prediction and write out the results
            //foreach (var c in result.Predictions)
            //{
            //    probabilities.Add($"{c.TagName}: {c.Probability:P1}");
            //}
        }
    }
}
