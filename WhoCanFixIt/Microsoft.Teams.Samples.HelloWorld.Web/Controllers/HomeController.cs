using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Microsoft.Teams.Samples.HelloWorld.Web.Controllers
{
    public class HomeController : Controller
    {
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
    }
}
