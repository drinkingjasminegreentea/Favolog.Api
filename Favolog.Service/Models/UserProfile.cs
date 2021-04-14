using System.Collections.Generic;

namespace Favolog.Service.Models
{
    public class UserProfile
    {
        public List<CatalogOverview> Catalogs { get; set; }

        public User User { get; set; }
        
    }
}
