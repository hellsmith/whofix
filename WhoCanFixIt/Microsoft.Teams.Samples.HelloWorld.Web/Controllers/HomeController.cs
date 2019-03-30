﻿using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
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

namespace Microsoft.Teams.Samples.HelloWorld.Web.Controllers
{
    public class HomeController : Controller
    {
        private const string trainingKey = "<your training key here>";
        private const string predictionKey = "<your prediction key here>";

        private static Guid PROJECT_ID = new Guid("WhoCanFixIt");
        private const string SouthCentralUsEndpoint = "https://southcentralus.api.cognitive.microsoft.com";
        
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

        public ActionResult Test(IEnumerable<HttpPostedFileBase> files)
        {

            return View();
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
                Endpoint = SouthCentralUsEndpoint
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
                Endpoint = SouthCentralUsEndpoint
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
