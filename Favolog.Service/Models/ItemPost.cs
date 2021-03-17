using Newtonsoft.Json;
using System;

namespace Favolog.Service.Models
{
    public class ItemPost
    {
        public int? CatalogId { get; set; }

        public string CatalogName { get; set; }

        public string Url { get; set; }

        public string OriginalUrl { get; set; }

        public string Title { get; set; }

        public string ImageName { get; set; }
        
    }
}
