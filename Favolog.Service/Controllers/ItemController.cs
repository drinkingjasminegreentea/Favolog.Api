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
        public async Task<ActionResult> Post([FromBody] Item item)
        {
            var catalog = _repository.Get<Catalog>(item.CatalogId).Include(c => c.User).SingleOrDefault();
            if (catalog == null)
                return BadRequest();

            if (!HttpContext.IsAuthorized(catalog.User.ExternalId))
                return Unauthorized();
                        
            if (!string.IsNullOrEmpty(item.OriginalUrl))
            {
                var openGraphInfo = await _openGraphGenerator.GetOpenGraph(item.OriginalUrl);
                item.SourceImageUrl = openGraphInfo.Image;
                item.Url = openGraphInfo.Url;
                item.OriginalUrl = item.OriginalUrl;
                item.Title = openGraphInfo.Title;
                item.ImageName = GetNewImageName(openGraphInfo.Image);
                _blobService.UploadItemImageFromUrl(openGraphInfo.Image, item.ImageName);
            }
            else
                return BadRequest();

            _repository.Attach(item);
            _repository.SaveChanges();

            return Ok(item);
        }

        [HttpPut]
        public Item Put([FromBody] Item product)
        {
            _repository.Attach(product);
            _repository.SaveChanges();

            return product;
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
