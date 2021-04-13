using System;

namespace Favolog.Service.Models
{
    public class UserFeedItem: Entity
    {
        public int CatalogId { get; set; }               

        public int UserId { get; set; }

        public string Username { get; set; }

        public string ProfileImage { get; set; }

        public string CatalogName { get; set; }

        public string Title { get; set; }

        public string ImageName { get; set; }

        public string Url { get; set; }

        public string UrlDomain
        {
            get
            {
                if (!string.IsNullOrEmpty(Url) && Uri.IsWellFormedUriString(Url, UriKind.Absolute))
                    return new Uri(Url).Host;
                return "";
            }
        }

        public string Comment { get; set; }

    }
}
