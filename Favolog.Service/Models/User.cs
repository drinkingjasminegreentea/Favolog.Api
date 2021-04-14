using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Favolog.Service.Models
{
    public class User: Entity
    {
        public string Username { get; set; }
                
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string EmailAddress { get; set; }

        public string ExternalId { get; set; }

        public string ProfileImage { get; set; }

        public string Bio { get; set; }

        public string Website { get; set; }        

        public List<Catalog> Catalogs { get; set; }

        public List<User> Followers { get; set; }

        public List<User> Following { get; set; }

        [NotMapped]
        public int TotalFollowing { get; set; }

        [NotMapped]
        public int TotalFollowers { get; set; }

        [NotMapped]
        public bool IsFollowing { get; set; }

        [NotMapped]
        public string DisplayName { get; set; }

    }
}
