using Favolog.Service.Models;
using Favolog.Service.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Favolog.Service.Controllers
{
    [ApiController]
    [Authorize(Policy = "access")]
    [Route("api/[controller]")]
    public class FeedController : ControllerBase
    {
        private readonly IFavologRepository _repository;
        public FeedController(IFavologRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [Route("user/{id}")]        
        public ActionResult GetUser([FromRoute] int id)
        {
            var user = _repository.Get<User>(id).SingleOrDefault();
            if (user == null)
                return NotFound();

            var feedUserIds = _repository.Get<UserFollow>().Where(f => f.FollowerId == user.Id).Select(f => f.UserId).ToList();
            feedUserIds.Add(user.Id.Value);

            var userFeed = new UserFeed
            {
                Items = _repository.Get<UserFeedItem>().Where(f => feedUserIds.Contains(f.UserId)).OrderByDescending(f => f.Id).ToList()
            };

            if (userFeed.Items.Count ==0)
            {
                userFeed.NewUser = true;
                userFeed.Items = _repository.Get<UserFeedItem>().OrderByDescending(f => f.Id).Take(9).ToList();
            }
            
            return Ok(userFeed);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Get()
        {
            var userFeed = new UserFeed
            {
                Items = _repository.Get<UserFeedItem>().OrderByDescending(f => f.Id).Take(30).ToList(),
                GuestUser = true
            };
            
            return Ok(userFeed);
        }
    }
}
