using System.ComponentModel.DataAnnotations;

namespace Favolog.Service.Models
{
    public class UserFollow: Entity
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int FollowerId { get; set; }
    }
}

