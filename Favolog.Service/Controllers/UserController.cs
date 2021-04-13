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
    public class UserController : ControllerBase
    {
        private readonly IFavologRepository _repository;
        public UserController(IFavologRepository repository)
        {
            _repository = repository;
        }        

        [HttpGet]                
        public ActionResult Get()
        {
            var loggedInUserId = HttpContext.GetLoggedInUserId();
            if (loggedInUserId == null)
                return Unauthorized();

            var user = _repository.Get<User>(loggedInUserId.Value).SingleOrDefault();
            if (user == null)
                return BadRequest();

            return Ok(user);
        }

        [HttpPost]
        public ActionResult Post([FromBody] User user)
        {
            var existingUser = _repository.Get<User>().Where(u => u.ExternalId == user.ExternalId).SingleOrDefault();
                        
            if (existingUser != null)
            {                
                return Ok(existingUser);
            }
            
            user.GenerateUsername(_repository);            
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

            var followingUserIds = _repository.Get<UserFollow>().Where(f => f.FollowerId == user.Id).Select(f => f.UserId).ToList();

            var result = _repository.Get<UserFeedItem>().Where(f => followingUserIds.Contains(f.UserId)).OrderByDescending(f=>f.Id).ToList();

            return Ok(result);
        }

        [HttpPost]
        [Route("Follow")]        
        public ActionResult Follow([FromBody] UserFollow userFollow)
        {
            if (string.IsNullOrEmpty(userFollow.Username) || string.IsNullOrEmpty(userFollow.FollowerUsername))
                return BadRequest("Username or FollowerUsername is empty");

            var user = _repository.Get<User>()
                .Where(u => u.Username == userFollow.Username).SingleOrDefault();

            var follower = _repository.Get<User>()
                .Where(u => u.Username == userFollow.FollowerUsername).SingleOrDefault();

            if (user == null || follower == null)
                return NotFound();

            var existingFollow = _repository.Get<UserFollow>()
                                    .Where(f => f.UserId == user.Id.Value && f.FollowerId == follower.Id.Value)
                                    .SingleOrDefault();

            if (existingFollow != null)
                _repository.Delete(existingFollow);
            else
            {
                _repository.Attach(new UserFollow { UserId = user.Id.Value, FollowerId = follower.Id.Value});
            }                

            _repository.SaveChanges();

            return Ok();                    
        }

        [HttpGet]
        [Route("{username}/followers")]
        [AllowAnonymous]
        public ActionResult Followers([FromRoute] string username)
        {
            var user = _repository.Get<User>().Where(u => u.Username == username).SingleOrDefault();
            if (user == null)
                return NotFound();

            var followerIds = _repository.Get<UserFollow>().Where(f => f.UserId == user.Id).Select(f => f.FollowerId).ToList();
            user.Followers = _repository.Get<User>().Where(u => followerIds.Contains(u.Id.Value)).ToList();

            return Ok(user);
        }

        [HttpGet]
        [Route("{username}/following")]
        [AllowAnonymous]
        public ActionResult Following([FromRoute] string username)
        {
            var user = _repository.Get<User>().Where(u => u.Username == username).SingleOrDefault();
            if (user == null)
                return NotFound();

            var followingIds = _repository.Get<UserFollow>().Where(f => f.FollowerId == user.Id).Select(f => f.UserId).ToList();
            user.Following = _repository.Get<User>().Where(u => followingIds.Contains(u.Id.Value)).ToList();

            return Ok(user);
        }


        [HttpPut]
        public ActionResult<User> Put([FromBody] User user)
        {
            var loggedInUserId = HttpContext.GetLoggedInUserId();
            if (loggedInUserId == null)
                return Unauthorized();

            var existingUser = _repository.Get<User>(loggedInUserId.Value).SingleOrDefault();
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

            return Ok(existingUser);
        }

        [HttpGet]
        [Route("{username}/public")]
        [AllowAnonymous]
        public ActionResult<UserProfile> GetPublicProfile([FromRoute] string username)
        {
            var user = _repository.Get<User>().Where(u => u.Username == username).SingleOrDefault();
            if (user == null)
                return BadRequest("Unable to find the user");

            var userProfile = GetUserProfile(user);
            return Ok(userProfile);
        }
                
        [HttpGet]
        [Route("{username}/private")]        
        public ActionResult<UserProfile> GetPrivateProfile([FromRoute] string username)
        {
            var loggedInUserId = HttpContext.GetLoggedInUserId();
            if (loggedInUserId == null)
                return Unauthorized();

            var user = _repository.Get<User>().Where(u => u.Username == username).SingleOrDefault();
            if (user == null)
                return BadRequest("Unable to find the user");

            var userProfile = GetUserProfile(user);
            userProfile.IsFollowing = _repository.Get<UserFollow>()
                                    .Any(f => f.UserId == user.Id.Value && f.FollowerId == loggedInUserId);

            return Ok(userProfile);
        }

        [HttpDelete]
        [Route("{username}")]
        public ActionResult Delete([FromRoute] string username)
        {
            var user = _repository.Get<User>().Where(u => u.Username == username)
                .Include(u=>u.Catalogs).ThenInclude(c=>c.Items)
                .SingleOrDefault();

            if (user == null)
                return BadRequest();

            var loggedInUserId = HttpContext.GetLoggedInUserId();
            if (loggedInUserId == null)
                return Unauthorized();

            if (user.Id.Value != loggedInUserId.Value)
                return Unauthorized();

            var items = user.Catalogs.SelectMany(c => c.Items);

            _repository.Delete(items);
            _repository.Delete(user.Catalogs);
            _repository.Delete(user);
            _repository.SaveChanges();

            return new NoContentResult();
        }

        private UserProfile GetUserProfile(User user)
        {
            var catalogs = _repository.Get<Catalog>()
                            .Include(c => c.Items)
                            .Where(c => c.UserId == user.Id.Value);

            var catalogsOverview = catalogs.Select(c =>
                new CatalogOverview
                {
                    Id = c.Id,
                    Name = c.Name,
                    ItemCount = c.Items.Count,
                    LastItemImage = c.Items.Where(item => !string.IsNullOrEmpty(item.ImageName)).OrderBy(item => item.Id)
                            .Select(item => item.ImageName).FirstOrDefault()
                }).ToList();

            var result = new UserProfile
            {
                user = user,
                Catalogs = catalogsOverview
            };

            result.TotalFollowers = _repository.Get<UserFollow>().Where(f => f.UserId == user.Id).Count();
            result.TotalFollowing = _repository.Get<UserFollow>().Where(f => f.FollowerId == user.Id).Count();
            return result;
        }

        [HttpGet]
        [Route("catalog")]
        public ActionResult<Catalog> GetCatalogs()
        {
            var loggedInUserId = HttpContext.GetLoggedInUserId();
            if (loggedInUserId == null)
                return Unauthorized();

            var user = _repository.Get<User>().Where(u => u.Id == loggedInUserId).SingleOrDefault();
            if (user == null)
                return Unauthorized();

            var catalogs = _repository.Get<Catalog>().Where(c => c.UserId == user.Id.Value).OrderBy(c => c.Name).ToList();

            return Ok(catalogs);
        }

    }
}
