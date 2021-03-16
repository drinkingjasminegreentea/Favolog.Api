using System;
using System.Collections.Generic;

namespace Favolog.Service.Models
{
    public class UserFeed: Entity
    {
        public IList<UserFeedItem> Items { get; set; }

        public bool GuestUser { get; set; }

        public bool NewUser { get; set; }

    }
}
