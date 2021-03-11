using Newtonsoft.Json;
using System;

namespace Favolog.Service.Models
{
    public class Item : Entity
    {
        public Catalog Catalog {get; set;}

        public int CatalogId { get; set; }

        public string Url { get; set; }

        public string OriginalUrl { get; set; }

        public string Title { get; set; }

        public string ImageName { get; set; }

        [JsonIgnore]        
        public string SourceImageUrl { get; set; }

        public string Comment { get; set; }

        public string UrlDomain { get
            {
                if (!string.IsNullOrEmpty(Url) && Uri.IsWellFormedUriString(Url, UriKind.Absolute))
                    return new Uri(Url).Host;
                return "";
            }
        }        
        
    }
}
