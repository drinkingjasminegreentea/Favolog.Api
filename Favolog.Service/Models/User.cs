using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Favolog.Service.Models
{
    public class User: Entity
    {
        [Required]
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string EmailAddress { get; set; }

        [Required]
        public string ExternalId { get; set; }

        public string ProfileImage { get; set; }

        public string Bio { get; set; }

        public string Website { get; set; }        

        public List<Catalog> Catalogs { get; set; }

        public List<User> Followers { get; set; }

        public List<User> Following { get; set; }
    }
}
