using System.Collections.Generic;

namespace Favolog.Service.Models
{
    public class UserProfile
    {
        public List<CatalogOverview> Catalogs { get; set; }

        public User user { get; set; }

        public int TotalFollowing { get; set; }

        public int TotalFollowers { get; set; }
    }
}
