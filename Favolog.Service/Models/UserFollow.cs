using System.ComponentModel.DataAnnotations.Schema;

namespace Favolog.Service.Models
{
    public class UserFollow: Entity
    {
        public User User { get; set; }

        public int UserId { get; set; }

        [NotMapped]
        public string Username { get; set; }
        
        public User Follower { get; set; }

        public int FollowerId { get; set; }

        [NotMapped]
        public string FollowerUsername { get; set; }
    }
}

