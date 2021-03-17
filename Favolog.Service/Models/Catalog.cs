using Favolog.Service.Models.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Favolog.Service.Models
{
    public class Catalog: Entity
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public AudienceTypes AudienceType { get; set; }

        [Required]
        public int UserId { get; set; }

        [NotMapped]
        public bool IsEditable { get; set; }

        public User User { get; set; }

        public List<Item> Items { get; set; }

        public Catalog()
        {
            Items = new List<Item>();
        }
    }
}
