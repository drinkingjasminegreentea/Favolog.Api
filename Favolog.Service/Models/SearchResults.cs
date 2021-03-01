using System.Collections.Generic;

namespace Favolog.Service.Models
{
    public class SearchResults
    {
        public List<User> Users { get; set; }

        public List<Catalog> Catalogs { get; set; }

        public List<Item> Items { get; set; }
    }
}
