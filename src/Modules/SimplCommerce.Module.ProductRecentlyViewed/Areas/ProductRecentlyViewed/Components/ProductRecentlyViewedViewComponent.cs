using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CRM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Infrastructure.Web;
using SimplCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Catalog.Services;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.ProductRecentlyViewed.Data;

namespace SimplCommerce.Module.ProductRecentlyViewed.Areas.ProductRecentlyViewed.Components
{
    public class ProductRecentlyViewedViewComponent : ViewComponent
    {
        private readonly IRecentlyViewedProductRepository _productRepository;
        private readonly IMediaService _mediaService;
        private readonly IProductPricingService _productPricingService;
        private readonly IWorkContext _workContext;
        private readonly IContentLocalizationService _contentLocalizationService;

        private readonly IRepository<Entity> _entityRepository;

        private readonly IRepositoryWithTypedId<EntityType, string> _entityTypeRepository;

        public ProductRecentlyViewedViewComponent(IRecentlyViewedProductRepository productRepository,
            IMediaService mediaService,
            IProductPricingService productPricingService,
            IContentLocalizationService contentLocalizationService,
            IWorkContext workContext, IRepository<Entity> entityRepository,
            IRepositoryWithTypedId<EntityType, string> entityTypeRepository)
        {
            _productRepository = productRepository;
            _mediaService = mediaService;
            _productPricingService = productPricingService;
            _contentLocalizationService = contentLocalizationService;
            _workContext = workContext;
            _entityRepository = entityRepository;
            _entityTypeRepository = entityTypeRepository;

        }

        // TODO Number of items to config
        public async Task<IViewComponentResult> InvokeAsync(long? productId, int itemCount = 4)
        {
            List<ProductThumbnail> model = new List<ProductThumbnail>();
            if (productId !=null)
            {
                var Prodcrossell = Crosssell(GetIP(), GetSession(), productId);
                foreach (var product in Prodcrossell.result)
                {
                    ProductThumbnail tm = new ProductThumbnail();
                    tm.Id = long.Parse(product.identifier);
                    tm.Name = product.description;
                    tm.ThumbnailUrl = product.imagelarge;
                    decimal pr = 0;
                    pr = decimal.Parse(product.pricewithtax.Replace(".", ","));
                    tm.Price = pr;
                    tm.Slug = tm.Name.Replace(" ", "-") + "-" + tm.Id;
                    Core.Models.Media pti = new ProductThumbnail().ThumbnailImage;
                    tm.ThumbnailUrl = _mediaService.GetThumbnailUrl(pti);
                    tm.ThumbnailUrl = _mediaService.GetURL(product.imagemedium);

                    //tm.CalculatedProductPrice(p);
                    //tm.CalculatedProductPrice = _productPricingService.CalculateProductPrice((decimal.Parse(p.pricewithtax)));
                    tm.CalculatedProductPrice = _productPricingService.CalculateProductPrice(tm);

                    model.Add(tm);
                    var entity = _entityRepository
                    .Query()
                    .Include(x => x.EntityType)
                    .FirstOrDefault(x => x.Slug == tm.Slug);
                    if (entity == null)
                    {
                        Entity en = new Entity();

                        en.EntityId = (long)tm.Id;
                        en.Name = tm.Name;
                        en.Slug = tm.Slug;
                        var enType = _entityTypeRepository.Query().FirstOrDefault(x => x.Id == "Product");
                        en.EntityType = enType;

                        //en.EntityType = (EntityType)enType;
                        //en.EntityType = new EntityType("Product");
                        //en.EntityType.AreaName = "Catalog";
                        //en.EntityType.IsMenuable = false;
                        //en.EntityType.RoutingController = "Product";
                        //en.EntityType.RoutingAction = "ProductDetail";
                        _entityRepository.Add(en);
                        _entityRepository.SaveChanges();
                    }

                }


            }
            //productos relacionados




            return View(this.GetViewPath(), model);

            // codigo origen
            //var user = await _workContext.GetCurrentUser();
            //IQueryable<Product> query = _productRepository.GetRecentlyViewedProduct(user.Id)
            //    .Include(x => x.ThumbnailImage);
            //if (productId.HasValue)
            //{
            //    query = query.Where(x => x.Id != productId.Value);
            //}

            //var model = query.Take(itemCount)
            //    .Select(x => ProductThumbnail.FromProduct(x)).ToList();

            //foreach (var product in model)
            //{
            //    product.Name = _contentLocalizationService.GetLocalizedProperty(nameof(Product), product.Id, nameof(product.Name), product.Name);
            //    product.ThumbnailUrl = _mediaService.GetThumbnailUrl(product.ThumbnailImage);
            //    product.CalculatedProductPrice = _productPricingService.CalculateProductPrice(product);
            //}

            //return View(this.GetViewPath(), model);
        }

        public string GetIP()
        {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString()));
        }
        public string GetSession()
        {
            string se = HttpContext.Session.GetString("idtk");
            if (se == null)
            {
                se = comunes.GetToken(GetIP());
                HttpContext.Session.SetString("idtk", se);
            }
            return HttpContext.Session.GetString("idtk");
        }
        public DataCollection<producto> Crosssell(string laip, string _sesionToken, long? identificador)
        {
            var result = new DataCollection<producto>();

            //resultList _area;
            // string token = GetToken(laip).activeToken;
            //string de url principal
            string urlPath = "https://riews.reinfoempresa.com:8443";
            //string codeidentifier = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(identificador.ToString()));
            string codeidentifier = identificador.ToString();

            //string de la url del método de llamada
            //https://riews.reinfoempresa.com:8443/RIEWS/webapi/PrivateServices/articles1
            string request2 = urlPath + "/RIEWS/webapi/PrivateServices/articles11W";
            //creamos un webRequest con el tipo de método.
            WebRequest webRequest = WebRequest.Create(request2);
            //definimos el tipo de webrequest al que llamaremos
            webRequest.Method = "POST";
            //definimos content\
            webRequest.ContentType = "application/json; charset=utf-8";
            //cargamos los datos a enviar
            using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
            {
                //string json = "{\"token\":\"" + token + "\",\"ipbase64\":\"" + laip +"}";
                string json = "{\"token\":\"" + _sesionToken + "\",\"ipbase64\":\"" + laip + "\",\"codeidentifier\":\"" + codeidentifier + "\"}";
                streamWriter.Write(json.ToString());
                //"  "
            }
            //obtenemos la respuesta del servidor
            var httpResponse = (HttpWebResponse)webRequest.GetResponse();
            //leemos la respuesta y la tratamos
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result2 = streamReader.ReadToEnd();
                //traducimos el resultado
                // result = JsonSerializer.Deserialize<DataCollectionSingle<producto>>(result2);
                result = JsonConvert.DeserializeObject<DataCollection<producto>>(result2);
            }
            //
            //if (_area == null)
            //{
            //    _area = new resultList();
            //    _area.result = new List<result>();
            //    _area.result[0].areaname = "vacia";
            //}

            return result;
        }

    }
}
