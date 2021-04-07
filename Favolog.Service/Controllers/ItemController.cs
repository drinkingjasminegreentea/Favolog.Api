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
        public async Task<ActionResult> Post([FromBody] Item item)
        {
            var loggedInUserId = HttpContext.GetLoggedInUserId();
            if (loggedInUserId == null)
                return Unauthorized();

            var userId = loggedInUserId.Value;

            var catalog = _repository.Get<Catalog>().Where(c => c.Id == item.CatalogId && c.UserId == userId).SingleOrDefault();
            if (catalog == null)
                return BadRequest("Catalog not found");

            if (!string.IsNullOrEmpty(item.OriginalUrl))
            {
                //when links are copied from Amazon, it addes item title before the URL, so need to strip that off
                var startIndex = item.OriginalUrl.IndexOf("https://");
                var correctedUrl = item.OriginalUrl.Substring(startIndex, item.OriginalUrl.Length - startIndex);
                var openGraphInfo = await _openGraphGenerator.GetOpenGraph(correctedUrl);
                item.SourceImageUrl = openGraphInfo.Image;
                item.Url = openGraphInfo.Url;                
                item.Title = openGraphInfo.Title;
                item.ImageName = GetNewImageName(openGraphInfo.Image);                
                _blobService.UploadItemImageFromUrl(openGraphInfo.Image, item.ImageName);
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
