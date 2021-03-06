using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Favolog.Service.Models
{
    public class Item: Entity
    {
        public string Title { get; set; }

        public string ImageName { get; set; }

        public string Url { get; set; }

        public string UrlDomain { get
            {
                if (!string.IsNullOrEmpty(Url))
                    return new Uri(Url).Host;
                return "";
            }
        }

        [JsonIgnore]
        public string SourceImageUrl { get; set; }

        [JsonIgnore]
        public string OriginalUrl { get; set; }

        public List<Catalog> Catalogs { get; set; }

        public List<CatalogItem> CatalogItems { get; set; }

    }
}
