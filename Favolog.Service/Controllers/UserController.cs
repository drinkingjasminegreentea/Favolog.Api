using Favolog.Service.Models;
using Favolog.Service.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Favolog.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IFavologRepository _repository;
        public UserController(IFavologRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [Route("{username}")]
        public User Get([FromRoute] string username)
        {
            return _repository.Get<User>()
                .Where(u => u.Username == username)
                .SingleOrDefault();
        }

        [HttpPost]
        public User Post([FromBody] User user)
        {
            var existingUser = _repository.Get<User>().Where(u => u.ExternalId == user.ExternalId).SingleOrDefault();
            if (existingUser != null)
                return existingUser;
            
            var username = user.EmailAddress.Substring(0, user.EmailAddress.IndexOf("@"));            
            existingUser = _repository.Get<User>().Where(u => u.Username == username).SingleOrDefault();
            if (existingUser != null)
            {
                existingUser.ExternalId = user.ExternalId;
                _repository.SaveChanges();
                return existingUser;
            }

            user.Username = username;

            _repository.Attach(user);
            _repository.SaveChanges();
            return user;
        }

        [HttpGet]
        [Route("{username}/feed")]
        public ActionResult GetFeed([FromRoute] string username)
        {
            var user = _repository.Get<User>().Where(u => u.Username == username).SingleOrDefault();
            if (user == null)
                return NotFound();

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
        [Route("{username}/followers")]
        public ActionResult Followers([FromRoute] string username)
        {
            var user = _repository.Get<User>().Where(u => u.Username == username).SingleOrDefault();
            if (user == null)
                return NotFound();

            var followerIds = _repository.Get<UserFollow>().Where(f => f.UserId == user.Id).Select(f => f.FollowerId).ToList();
            var users = _repository.Get<User>().Where(u => followerIds.Contains(u.Id.Value)).ToList();

            return Ok(users);
        }

        [HttpGet]
        [Route("{username}/following")]
        public ActionResult Following([FromRoute] string username)
        {
            var user = _repository.Get<User>().Where(u => u.Username == username).SingleOrDefault();
            if (user == null)
                return NotFound();

            var followingIds = _repository.Get<UserFollow>().Where(f => f.FollowerId == user.Id).Select(f => f.UserId).ToList();
            var users = _repository.Get<User>().Where(u => followingIds.Contains(u.Id.Value)).ToList();

            return Ok(users);
        }


        [HttpPut]
        public ActionResult<User> Put([FromBody] User user)
        {
            var existingUser = _repository.Get<User>(user.Id.Value).SingleOrDefault();
            if (existingUser == null)
                return BadRequest();

            existingUser.Username = user.Username;
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
        [Route("{username}/profile")]
        public ActionResult<UserProfile> GetProfile([FromRoute] string username)
        {
            if (string.IsNullOrEmpty(username))
                return BadRequest();

            var user = _repository.Get<User>().Where(user => user.Username == username).SingleOrDefault();
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
    }
}
