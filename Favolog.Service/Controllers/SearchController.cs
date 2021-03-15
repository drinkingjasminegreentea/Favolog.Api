using Favolog.Service.Models;
using Favolog.Service.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Favolog.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IFavologRepository _repository;
        public SearchController(IFavologRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]        
        public ActionResult<SearchResults> Get([FromQuery] string query)
        {
            var searchResults = new SearchResults { 
                Catalogs = _repository.Get<Catalog>().Include(c => c.User).Where(item => item.Name.Contains(query)).ToList(),                
                Users = _repository.Get<User>().Where(item => item.FirstName.Contains(query) || item.LastName.Contains(query) || item.Username.Contains
                (query)).ToList()
            };

            return searchResults;
        }
    }
}
