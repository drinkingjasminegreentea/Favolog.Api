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
    public class CatalogController : ControllerBase
    {
        private readonly IFavologRepository _repository;
        public CatalogController(IFavologRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [Route("{id}/public")]
        [AllowAnonymous]
        public ActionResult<Catalog> GetPublic([FromRoute] int id)
        {
            var catalog = _repository.Get<Catalog>(id)
                .Include(p => p.Items.OrderByDescending(i => i.Id))                
                .Include(p => p.User)
                .SingleOrDefault();

            if (catalog == null)
                return BadRequest();

            var user = _repository.Get<User>(catalog.UserId).SingleOrDefault();            

            return Ok(catalog);
        }

        [HttpGet]
        [Route("{id}")]        
        public ActionResult<Catalog> Get([FromRoute] int id)
        {
            var loggedInUserId = HttpContext.GetLoggedInUserId();
            if (loggedInUserId == null)
                return Unauthorized();

            var catalog = _repository.Get<Catalog>(id)
                .Include(p => p.Items.OrderByDescending(i => i.Id))
                .Include(p => p.User)
                .SingleOrDefault();

            if (catalog == null)
                return BadRequest();

            catalog.IsEditable = loggedInUserId == catalog.User.ExternalId;

            return Ok(catalog);
        }


        [HttpPost]        
        public ActionResult<Catalog> Post([FromBody] Catalog catalog)
        {
            var loggedInUserId = HttpContext.GetLoggedInUserId();
            if (loggedInUserId == null)
                return Unauthorized();

            var user = _repository.Get<User>()                
                .Where(u => u.ExternalId == loggedInUserId).SingleOrDefault();

            if (user == null)
                return BadRequest("User not found");
                                               
            var existingOne = _repository.Get<Catalog>()
                .Where(c => c.Name == catalog.Name && c.UserId == user.Id)
                .SingleOrDefault();

            if (existingOne != null)
                return BadRequest("Duplicate catalog name");

            catalog.UserId = user.Id.Value;

            _repository.Attach(catalog);
            _repository.SaveChanges();

            return Ok(catalog);
        }               

        [HttpPut]
        public ActionResult Put([FromBody] Catalog catalog)
        {
            var existingOne = _repository.Get<Catalog>(catalog.Id.Value).Include(c => c.User).SingleOrDefault();
            if (existingOne == null)
                return BadRequest();

            if (!HttpContext.IsAuthorized(existingOne.User.ExternalId))
                return Unauthorized();

            existingOne.Name = catalog.Name;
            _repository.SaveChanges();

            return Ok(existingOne);
        }

        [HttpDelete]
        [Route("{id}")]
        public ActionResult Delete([FromRoute] int id)
        {
            var catalog = _repository.Get<Catalog>(id).Include(c => c.User).Include(c => c.Items).SingleOrDefault();
            if (catalog == null)
                return BadRequest();

            if (!HttpContext.IsAuthorized(catalog.User.ExternalId))
                return Unauthorized();

            _repository.Delete(catalog.Items);
            _repository.Delete(catalog);
            _repository.SaveChanges();

            return new NoContentResult();
        }

        [HttpDelete]
        [Route("{id}/item/{itemId}")]
        public ActionResult DeleteItem([FromRoute] int id, [FromRoute] int itemId)
        {
            var catalog = _repository.Get<Catalog>(id).Include(c => c.User).SingleOrDefault();
            if (catalog == null)
                return BadRequest();

            if (!HttpContext.IsAuthorized(catalog.User.ExternalId))
                return Unauthorized();

            var item = _repository.Get<Item>().Where(ci => ci.CatalogId == id && ci.Id == itemId).SingleOrDefault();

            _repository.Delete(item);            
            _repository.SaveChanges();

            return Ok(item);
        }
    }
}
