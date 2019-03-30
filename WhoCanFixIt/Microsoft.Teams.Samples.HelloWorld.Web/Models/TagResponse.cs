using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.Teams.Samples.HelloWorld.Web.Models {
    public class TagResponse {
        public List<Guid> TagIds { get; set; }
        public List<string> TagNames { get; set; }
        public string ImgBase64 { get; set; }
    }
}