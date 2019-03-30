using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.Teams.Samples.HelloWorld.Web.Models {
    public class TagPrediction {
        public Guid TagId { get; set; }
        public string TagName { get; set; }
        public string TagDesc { get; set; }
        public double TagProbability { get; set; }
    }
}