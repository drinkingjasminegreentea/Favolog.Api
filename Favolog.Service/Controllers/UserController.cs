using Favolog.Service.Extensions;
using Favolog.Service.Models;
using Favolog.Service.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.RegularExpressions;

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
        [AllowAnonymous]
        public User Get([FromRoute] string username)
        {
            return _repository.Get<User>().Where(u => u.Username == username)
                .SingleOrDefault();
        }

        [HttpPost]
        public ActionResult Post([FromBody] User user)
        {
            var existingUser = _repository.Get<User>().Where(u => u.ExternalId == user.ExternalId).SingleOrDefault();
                        
            if (existingUser != null)
            {                
                return Ok(existingUser);
            }
            
            user.Username = GenerateUsername(user);
            user.IsNew = true;

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
            var existingFollow = _repository.Get<UserFollow>()
                                    .Where(f => f.User.Username == userFollow.Username && f.Follower.Username == userFollow.FollowerUsername)
                                    .SingleOrDefault();

            if (existingFollow != null)
                _repository.Delete(existingFollow);
            else
                _repository.Attach(userFollow);

            _repository.SaveChanges();

            return Ok();                    
        }

        [HttpGet]
        [Route("{followerUsername}/IsFollowing/{username}")]
        public ActionResult IsFollowing([FromRoute] string followerUsername, [FromRoute] string username)
        {
            var isFollowing = _repository.Get<UserFollow>()
                                    .Any(f => f.User.Username == username && f.Follower.Username == followerUsername);                                    
                        
            return Ok(isFollowing);
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
        [Route("{username}/profile")]
        [AllowAnonymous]
        public ActionResult<UserProfile> GetProfile([FromRoute] string username)
        {
            var user = _repository.Get<User>().Where(u => u.Username == username).SingleOrDefault();
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
                    ItemCount = c.Items.Count,
                    LastThreeImages = c.Items.Where(item => !string.IsNullOrEmpty(item.ImageName))
                                                .OrderByDescending(i => i.Title).Select(item=>item.ImageName).Take(3).ToList()
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

        private string GenerateUsername(User user)
        {
            var usernameRegex = new Regex(@"^[a-zA-Z0-9_]*$");
            string username = string.Empty;

            //generate username using display name
            if (string.IsNullOrEmpty(user.DisplayName))
            {
                username = user.DisplayName.Replace(" ", string.Empty).Replace("'", string.Empty).Replace("-", string.Empty);
            }

            // or generate username using email
            if (string.IsNullOrEmpty(username) || !usernameRegex.IsMatch(username))
            {
                if (!string.IsNullOrEmpty(user.EmailAddress))
                    username = user.EmailAddress.Substring(0, user.EmailAddress.IndexOf('@'));
            }

            // or generate default username
            if (string.IsNullOrEmpty(username) || !usernameRegex.IsMatch(username))
            {
                username = "user";
            }

            var existingCount = _repository.Get<User>().Where(u => u.Username == username).Count();
            if (existingCount > 0)
            {
                username = $"{username}{existingCount + 1}";
            }

            return username;
        }
    }
}
