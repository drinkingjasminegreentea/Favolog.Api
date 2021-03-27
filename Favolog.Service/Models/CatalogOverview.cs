using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Favolog.Service.Models
{
    public class CatalogOverview : Entity
    {
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string AudienceType { get; set; }

        public int ItemCount { get; set; }

        public List<string> LastThreeImages { get; set; }
    }
}
