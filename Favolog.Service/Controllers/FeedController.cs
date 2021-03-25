using Favolog.Service.Models;
using Favolog.Service.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Favolog.Service.Controllers
{
    [ApiController]
    [Authorize(Policy = "access")]
    [Route("api/[controller]")]
    public class FeedController : ControllerBase
    {
        private readonly IFavologRepository _repository;
        private const int _pageSize = 12;

        public FeedController(IFavologRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [Route("user/{id}")]        
        public async Task<ActionResult> GetUser([FromRoute] int id, [FromQuery] int? pageSize, [FromQuery] int? pageIndex)
        {
            var user = _repository.Get<User>(id).SingleOrDefault();
            if (user == null)
                return NotFound();

            if (!pageSize.HasValue)
                pageSize = 6;
            if (!pageIndex.HasValue)
                pageIndex = 1;

            var feedUserIds = _repository.Get<UserFollow>().Where(f => f.FollowerId == user.Id).Select(f => f.UserId).ToList();
            feedUserIds.Add(user.Id.Value);

            var items = _repository.Get<UserFeedItem>().Where(f => feedUserIds.Contains(f.UserId))
                .OrderByDescending(f => f.Id).Skip((pageIndex.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();

            return Ok(items);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> Get([FromQuery] int? pageSize, [FromQuery] int? pageIndex)
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
