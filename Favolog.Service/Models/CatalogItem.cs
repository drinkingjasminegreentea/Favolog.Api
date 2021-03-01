namespace Favolog.Service.Models
{
    public class CatalogItem: Entity
    {
        public int CatalogId { get; set; }

        public Catalog Catalog { get; set; }

        public int ItemId { get; set; }        

        public Item Item { get; set; }

        public string Comments { get; set; }
    }
}
