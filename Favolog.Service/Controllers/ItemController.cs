using Favolog.Service.Extensions;
using Favolog.Service.Models;
using Favolog.Service.Repository;
using Favolog.Service.ServiceClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Favolog.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "access")]
    public class ItemController : ControllerBase
    {
        private readonly IFavologRepository _repository;
        private readonly IBlobStorageService _blobService;
        private readonly IOpenGraphGenerator _openGraphGenerator;

        public ItemController(IFavologRepository repository, IBlobStorageService blobService, 
            IOpenGraphGenerator openGraphGenerator)
        {
            _repository = repository;
            _blobService = blobService;
            _openGraphGenerator = openGraphGenerator;
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

            var user = _repository.Get<User>()
                .Where(u => u.ExternalId == loggedInUserId).SingleOrDefault();

            if (user == null)
                return BadRequest("User not found");

            Catalog existingCatalog = null;
            if (itemPost.CatalogId != null)
            {
                existingCatalog = _repository.Get<Catalog>().Where(c => c.Id == itemPost.CatalogId && c.UserId == user.Id.Value).SingleOrDefault();
                if (existingCatalog == null)
                    return BadRequest("Catalog not found");
            }

            var item = new Item();
                                  
            if (!string.IsNullOrEmpty(itemPost.OriginalUrl))
            {
                //when links are copied from Amazon, it addes item title before the URL, so need to strip that off
                var startIndex = itemPost.OriginalUrl.IndexOf("https://");
                var correctedUrl = itemPost.OriginalUrl.Substring(startIndex, itemPost.OriginalUrl.Length - startIndex);
                var openGraphInfo = await _openGraphGenerator.GetOpenGraph(correctedUrl);
                item.SourceImageUrl = openGraphInfo.Image;
                item.Url = openGraphInfo.Url;
                item.OriginalUrl = itemPost.OriginalUrl;
                item.Title = openGraphInfo.Title;
                item.ImageName = GetNewImageName(openGraphInfo.Image);                
                _blobService.UploadItemImageFromUrl(openGraphInfo.Image, item.ImageName);
            }
            else
            {                
                item.Url = itemPost.Url;                
                item.Title = itemPost.Title;
                item.ImageName = itemPost.ImageName;
            }

            if (!string.IsNullOrEmpty(itemPost.CatalogName))
            {
                var newCatalog = new Catalog { UserId = user.Id.Value, AudienceType = Models.Enums.AudienceTypes.Public, Name = itemPost.CatalogName };
                _repository.Attach(newCatalog);                
                newCatalog.Items.Add(item);                
            }
            else
            {                
                item.CatalogId = existingCatalog.Id.Value;                
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

            if (!HttpContext.IsAuthorized(existingItem.Catalog.User.ExternalId))
                return Unauthorized();

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
