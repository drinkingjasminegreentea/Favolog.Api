namespace Favolog.Service.Models
{
    public class UserFeed
    {
        public PaginatedList<UserFeedItem> Page { get; set; }

        public bool GuestUser { get; set; }

        public bool NewUser { get; set; }

    }
}
