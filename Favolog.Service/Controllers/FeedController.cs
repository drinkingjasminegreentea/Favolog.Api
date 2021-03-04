using Favolog.Service.Models;
using Favolog.Service.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Favolog.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedController : ControllerBase
    {
        private readonly IFavologRepository _repository;
        public FeedController(IFavologRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [Route("user/{username}")]
        public ActionResult GetUser([FromRoute] string username)
        {
            var user = _repository.Get<User>().Where(u => u.Username == username).SingleOrDefault();
            if (user == null)
                return NotFound();

            var followingUserIds = _repository.Get<UserFollow>().Where(f => f.FollowerId == user.Id).Select(f => f.UserId).ToList();

            var result = _repository.Get<UserFeed>().Where(f => followingUserIds.Contains(f.UserId)).OrderByDescending(f=>f.Id).ToList();

            return Ok(result);
        }

        [HttpGet]        
        public ActionResult Get()
        {
            var result = _repository.Get<UserFeed>().OrderByDescending(f => f.ItemId).Take(20).ToList();

            return Ok(result);
        }
    }
}
