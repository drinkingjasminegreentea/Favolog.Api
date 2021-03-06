using Favolog.Service.Models;
using Favolog.Service.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Favolog.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CatalogController : ControllerBase
    {
        private readonly IFavologRepository _repository;
        public CatalogController(IFavologRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [Route("{id}")]
        public ActionResult<Catalog> Get([FromRoute] int id)
        {
            var catalog = _repository.Get<Catalog>(id)
                .Include(p => p.Items)     
                .Include(p => p.User)
                .SingleOrDefault();

            if (catalog == null)
                return BadRequest();

            var user = _repository.Get<User>(catalog.UserId).SingleOrDefault();            

            return Ok(catalog);
        }               

        [HttpPost]        
        public ActionResult<Catalog> Post([FromBody] Catalog catalog)
        {
            var user = _repository.Get<User>()                
                .Where(u => u.Id == catalog.UserId).SingleOrDefault();

            if (user == null)
                return BadRequest("User not found");
                        
            var existingOne = _repository.Get<Catalog>()
                .Where(c => c.Name == catalog.Name && c.UserId == user.Id)
                .SingleOrDefault();

            if (existingOne != null)
                return BadRequest("Duplicate catalog name");

            _repository.Attach(catalog);
            _repository.SaveChanges();

            return Ok(catalog);
        }               

        [HttpPut]
        public ActionResult Put([FromBody] Catalog catalog)
        {
            var existingOne = _repository.Get<Catalog>(catalog.Id.Value).SingleOrDefault();
            if (existingOne == null)
                return BadRequest();

            existingOne.Name = catalog.Name;
            _repository.SaveChanges();

            return Ok(existingOne);
        }

        [HttpDelete]
        [Route("{id}")]
        public ActionResult Delete([FromRoute] int id)
        {
            var catalog = _repository.Get<Catalog>(id).SingleOrDefault();
            if (catalog == null)
                return BadRequest();

            var catalogItems = _repository.Get<CatalogItem>().Where(ci => ci.CatalogId == id).AsEnumerable();

            _repository.Delete(catalogItems);
            _repository.Delete(catalog);
            _repository.SaveChanges();

            return new NoContentResult();
        }

        [HttpDelete]
        [Route("{id}/item/{itemId}")]
        public ActionResult DeleteItem([FromRoute] int id, [FromRoute] int itemId)
        {
            var catalog = _repository.Get<Catalog>(id).SingleOrDefault();
            if (catalog == null)
                return BadRequest();

            var catalogItem = _repository.Get<CatalogItem>().Where(ci => ci.CatalogId == id && ci.ItemId == itemId).SingleOrDefault();

            _repository.Delete(catalogItem);            
            _repository.SaveChanges();

            return new NoContentResult();
        }
    }
}
