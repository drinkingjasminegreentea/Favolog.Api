using Favolog.Service.Extensions;
using Favolog.Service.Models;
using Favolog.Service.Repository;
using Microsoft.AspNetCore.Authorization;
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
        [Route("user")]
        public ActionResult GetUser([FromQuery] int? pageSize, [FromQuery] int? pageIndex)
        {
            var loggedInUserId = HttpContext.GetLoggedInUserId();
            if (loggedInUserId == null)
                return Unauthorized();

            var userId = loggedInUserId.Value;

            if (!pageSize.HasValue)
                pageSize = 6;
            if (!pageIndex.HasValue)
                pageIndex = 1;

            var feedUserIds = _repository.Get<UserFollow>().Where(f => f.FollowerId == userId).Select(f => f.UserId).ToList();
            feedUserIds.Add(userId);

            var items = _repository.Get<UserFeedItem>().Where(f => feedUserIds.Contains(f.UserId))
                .OrderByDescending(f => f.Id).Skip((pageIndex.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();

            return Ok(items);
        }

        [HttpGet]
        [Route("profile/{username}")]
        [AllowAnonymous]
        public ActionResult GetProfile([FromRoute] string username, [FromQuery] int? pageSize, [FromQuery] int? pageIndex)
        {
            var user = _repository.Get<User>().Where(u => u.Username == username).SingleOrDefault();
            if (user == null)
                return NotFound();

            if (!pageSize.HasValue)
                pageSize = 6;
            if (!pageIndex.HasValue)
                pageIndex = 1;
                        
            var items = _repository.Get<UserFeedItem>().Where(f => f.UserId == user.Id.Value)
                .OrderByDescending(f => f.Id).Skip((pageIndex.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();

            return Ok(items);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Get([FromQuery] int? pageSize, [FromQuery] int? pageIndex)
        {
            if (!pageSize.HasValue)
                pageSize = 6;
            if (!pageIndex.HasValue)
                pageIndex = 1;

            var items = _repository.Get<UserFeedItem>().OrderByDescending(f => f.Id).Skip((pageIndex.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();

            return Ok(items);
        }
    }
}
