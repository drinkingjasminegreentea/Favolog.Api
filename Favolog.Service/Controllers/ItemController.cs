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
                .Include(i => i.Catalogs)
                .Include(i => i.CatalogItems)
                .SingleOrDefault();
        }             

        [HttpPost]        
        public async Task<ActionResult> Post([FromBody] ItemPost itemPost)
        {
            var catalog = _repository.Get<Catalog>(itemPost.CatalogId).SingleOrDefault();

            if (catalog == null)
                return BadRequest("Cannot find the catalog");

            var newItem = new Item();

            if (!string.IsNullOrEmpty(itemPost.Title))
            {
                newItem.Title = itemPost.Title;
                if (!string.IsNullOrEmpty(itemPost.ImageName))
                    newItem.ImageName = GetNewImageName(itemPost.ImageName);
                newItem.Url = itemPost.Url;
            }
            else if (!string.IsNullOrEmpty(itemPost.OriginalUrl))
            {
                var openGraphInfo = await _openGraphGenerator.GetOpenGraph(itemPost.OriginalUrl);
                newItem.SourceImageUrl = openGraphInfo.Image;
                newItem.Url = openGraphInfo.Url;
                newItem.OriginalUrl = itemPost.OriginalUrl;
                newItem.Title = openGraphInfo.Title;
                newItem.ImageName = GetNewImageName(openGraphInfo.Image);
                _blobService.UploadItemImageFromUrl(openGraphInfo.Image, newItem.ImageName);
            }
            else
                return BadRequest();

            var catalogItem = new CatalogItem
            {
                Catalog = catalog,
                Item = newItem,
                Comments = itemPost.Comments
            };

            _repository.Attach(newItem);
            _repository.Attach(catalogItem);

            _repository.SaveChanges();

            return Ok();

        }

        [HttpPut]
        public Item Put([FromBody] Item product)
        {
            _repository.Attach(product);
            _repository.SaveChanges();

            return product;
        }

        [HttpDelete]
        [Route("{id}")]
        public ActionResult Delete([FromRoute] int id)
        {
            var item = _repository.Get<Item>(id)                
                .SingleOrDefault();

            if (item == null)
                return BadRequest();

            var catalogItems = _repository.Get<CatalogItem>().Where(ci => ci.ItemId == id).AsEnumerable();
            _repository.Delete(catalogItems);

            _repository.Delete(item);
            _repository.SaveChanges();

            return new NoContentResult();
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
