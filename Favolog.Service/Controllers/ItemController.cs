using Favolog.Service.Extensions;
using Favolog.Service.Models;
using Favolog.Service.Repository;
using Favolog.Service.ServiceClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Favolog.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]    
    public class ItemController : ControllerBase
    {
        private readonly IFavologRepository _repository;
        private readonly IBlobStorageService _blobService;        

        public ItemController(IFavologRepository repository, IBlobStorageService blobService)
        {
            _repository = repository;
            _blobService = blobService;            
        }

        [HttpGet]
        [Route("{id}")]
        public Item Get([FromRoute] int id)
        {
            return _repository.Get<Item>(id)
                .Include(i => i.Catalog)
                .SingleOrDefault();
        }             

        [HttpPost]        
        public async Task<ActionResult> Post([FromBody] ItemPost itemPost)
        {
            var loggedInUserId = HttpContext.GetLoggedInUserId();
            if (loggedInUserId == null)
                return Unauthorized();

            var userId = loggedInUserId.Value;
            Catalog catalog=null;

            if (!itemPost.CatalogId.HasValue && string.IsNullOrEmpty(itemPost.CatalogName))
                return BadRequest("Create a new catalog or choose an existing one");

            //if it's an existing catalog, check if user has access to it
            if (itemPost.CatalogId.HasValue)
            {
                catalog = _repository.Get<Catalog>().Where(c => c.Id == itemPost.CatalogId && c.UserId == userId).SingleOrDefault();
                if (catalog == null)
                    return BadRequest("Catalog not found");
            }

            // if new catalog, then create it
            if (!string.IsNullOrEmpty(itemPost.CatalogName))
            {
                catalog = new Catalog
                {
                    Name = itemPost.CatalogName,
                    UserId = userId
                };
                _repository.Attach(catalog);
                _repository.SaveChanges();
            }

            //create the new item now
            var item = new Item
            {
                SourceImageUrl = itemPost.SourceImageUrl,
                OriginalUrl = itemPost.OriginalUrl,
                Url = itemPost.Url,
                Title = itemPost.Title,
                ImageName = itemPost.ImageName,
                CatalogId = catalog.Id.Value
            };

            // if item has source image url, then copy it
            if (!string.IsNullOrEmpty(itemPost.SourceImageUrl))
            {
                 item.ImageName = GetNewImageName(itemPost.SourceImageUrl);
                _blobService.UploadItemImageFromUrl(itemPost.SourceImageUrl, item.ImageName);
            }
            
            _repository.Attach(item);
            _repository.SaveChanges();

            return Ok(item);
        }

        [HttpPut]
        public ActionResult Put([FromBody] Item item)
        {
            var existingItem = _repository.Get<Item>(item.Id).Include(i => i.Catalog).ThenInclude(c=>c.User).SingleOrDefault();
            if (existingItem == null)
                return BadRequest();

            existingItem.Title = item.Title;
            if (!string.IsNullOrEmpty(item.Url))
                existingItem.Url = item.Url;

            existingItem.Comment = item.Comment;

            if (!string.IsNullOrEmpty(item.ImageName))
                existingItem.ImageName = item.ImageName;

            _repository.SaveChanges();

            return Ok(existingItem);
        }
        
        private string GetNewImageName(string imageUrl)
        {
            var extension = GetFileExtensionFromUrl(imageUrl);
            var newName = $"{Guid.NewGuid()}{extension}";

            return newName;
        }

        public static string GetFileExtensionFromUrl(string url)
        {
            url = url.Split('?')[0];
            url = url.Split('/').Last();
            return url.Contains('.') ? url.Substring(url.LastIndexOf('.')) : "";
        }
    }
}
