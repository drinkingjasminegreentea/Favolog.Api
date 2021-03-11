using Favolog.Service.Extensions;
using Favolog.Service.Models;
using Favolog.Service.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Favolog.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "access")]
    public class UserController : ControllerBase
    {
        private readonly IFavologRepository _repository;
        public UserController(IFavologRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [Route("{id}")]
        [AllowAnonymous]
        public User Get([FromRoute] int id)
        {
            return _repository.Get<User>(id)                
                .SingleOrDefault();
        }

        [HttpPost]
        public ActionResult Post([FromBody] User user)
        {
            if (!HttpContext.IsAuthorized(user.ExternalId))
                return Unauthorized();

            var existingUser = _repository.Get<User>().Where(u => u.ExternalId == user.ExternalId).SingleOrDefault();

            if (existingUser != null)
            {                
                return Ok(existingUser);
            }              
            
            _repository.Attach(user);
            _repository.SaveChanges();
            return Ok(user); 
        }

        [HttpGet]
        [Route("{id}/feed")]
        public ActionResult GetFeed([FromRoute] int id)
        {
            var user = _repository.Get<User>(id).SingleOrDefault();
            if (user == null)
                return NotFound();

            if (!HttpContext.IsAuthorized(user.ExternalId))
                return Unauthorized();

            var followingUserIds = _repository.Get<UserFollow>().Where(f => f.FollowerId == user.Id).Select(f => f.UserId).ToList();

            var result = _repository.Get<UserFeed>().Where(f => followingUserIds.Contains(f.UserId)).OrderByDescending(f=>f.Id).ToList();

            return Ok(result);
        }

        [HttpPost]
        [Route("Follow")]        
        public ActionResult Follow([FromBody] UserFollow userFollow)
        {
            var existingFollow = _repository.Get<UserFollow>()
                                    .Where(f => f.UserId == userFollow.UserId && f.FollowerId == userFollow.FollowerId)
                                    .SingleOrDefault();

            if (existingFollow != null)
                _repository.Delete(existingFollow);
            else
                _repository.Attach(userFollow);

            _repository.SaveChanges();

            return Ok();                    
        }

        [HttpGet]
        [Route("{followerId}/IsFollowing/{userId}")]
        public ActionResult IsFollowing([FromRoute] int followerId, [FromRoute] int userId)
        {
            var isFollowing = _repository.Get<UserFollow>()
                                    .Any(f => f.UserId == userId && f.FollowerId == followerId);                                    
                        
            return Ok(isFollowing);
        }

        [HttpGet]
        [Route("{id}/followers")]
        [AllowAnonymous]
        public ActionResult Followers([FromRoute] int id)
        {
            var user = _repository.Get<User>(id).SingleOrDefault();
            if (user == null)
                return NotFound();

            var followerIds = _repository.Get<UserFollow>().Where(f => f.UserId == user.Id).Select(f => f.FollowerId).ToList();
            user.Followers = _repository.Get<User>().Where(u => followerIds.Contains(u.Id.Value)).ToList();

            return Ok(user);
        }

        [HttpGet]
        [Route("{id}/following")]
        [AllowAnonymous]
        public ActionResult Following([FromRoute] int id)
        {
            var user = _repository.Get<User>(id).SingleOrDefault();
            if (user == null)
                return NotFound();

            var followingIds = _repository.Get<UserFollow>().Where(f => f.FollowerId == user.Id).Select(f => f.UserId).ToList();
            user.Following = _repository.Get<User>().Where(u => followingIds.Contains(u.Id.Value)).ToList();

            return Ok(user);
        }


        [HttpPut]
        public ActionResult<User> Put([FromBody] User user)
        {
            var existingUser = _repository.Get<User>(user.Id.Value).SingleOrDefault();
            if (existingUser == null)
                return BadRequest();

            if (!HttpContext.IsAuthorized(existingUser.ExternalId))
                return Unauthorized();
                        
            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            existingUser.EmailAddress = user.EmailAddress;
            existingUser.Bio = user.Bio;
            existingUser.Website = user.Website;
            if (!string.IsNullOrEmpty(user.ProfileImage))
            {
                existingUser.ProfileImage = user.ProfileImage;
            }
            else
            {
                user.ProfileImage = existingUser.ProfileImage;
            }

            _repository.SaveChanges();            

            return user;
        }

        [HttpGet]
        [Route("{id}/profile")]
        [AllowAnonymous]
        public ActionResult<UserProfile> GetProfile([FromRoute] int id)
        {
            var user = _repository.Get<User>(id).SingleOrDefault();
            if (user == null)
                return BadRequest("Unable to find the user");

            var catalogs = _repository.Get<Catalog>()
                .Include(c => c.Items)
                .Where(c => c.UserId == user.Id.Value);

            var catalogsOverview = catalogs.Select(c =>
                new CatalogOverview
                {
                    Id = c.Id,
                    Name = c.Name,
                    AudienceType = c.AudienceType.ToString(),
                    ItemCount = c.Items.Count,
                    LastItemImage = c.Items.OrderByDescending(i => i.Title).LastOrDefault().ImageName
                }).ToList();

            var result = new UserProfile
            {
                user = user,
                Catalogs = catalogsOverview
            };

            result.TotalFollowers = _repository.Get<UserFollow>().Where(f => f.UserId == user.Id).Count();
            result.TotalFollowing = _repository.Get<UserFollow>().Where(f => f.FollowerId == user.Id).Count();
            return Ok(result);
        }

        [HttpDelete]
        [Route("{id}")]
        public ActionResult Delete([FromRoute] int id)
        {
            var user = _repository.Get<User>(id).Include(u=>u.Catalogs).ThenInclude(c=>c.Items).SingleOrDefault();
            if (user == null)
                return BadRequest();

            if (!HttpContext.IsAuthorized(user.ExternalId))
                return Unauthorized();

            var items = user.Catalogs.SelectMany(c => c.Items);

            _repository.Delete(items);
            _repository.Delete(user.Catalogs);
            _repository.Delete(user);
            _repository.SaveChanges();

            return new NoContentResult();
        }
    }
}
