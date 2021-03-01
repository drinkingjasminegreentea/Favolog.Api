using System.ComponentModel.DataAnnotations;

namespace Favolog.Service.Models
{
    public class User: Entity
    {
        public string Username { get; set; }        
               
        public string FirstName { get; set; }

        public string LastName { get; set; }

        [Required]
        public string EmailAddress { get; set; }        

        public string ExternalId { get; set; }

        public string ProfileImage { get; set; }

        public string Bio { get; set; }

        public string Website { get; set; }        
    }
}
