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
using SimplCommerce.Module.Core.Areas.Core.ViewModels;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.Catalog.Areas.Catalog.Components
{
    public class ProductWidgetViewComponent : ViewComponent
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IMediaService _mediaService;
        private readonly IProductPricingService _productPricingService;
        private readonly IContentLocalizationService _contentLocalizationService;
        private readonly IRepository<Entity> _entityRepository;
        private readonly IRepositoryWithTypedId<EntityType, string> _entityTypeRepository;
        public ProductWidgetViewComponent(IRepository<Product> productRepository,
            IMediaService mediaService,
            IProductPricingService productPricingService,
            IContentLocalizationService contentLocalizationService,
            IRepository<Entity> entityRepository,
            IRepositoryWithTypedId<EntityType, string> entityTypeRepository)
        {
            _productRepository = productRepository;
            _mediaService = mediaService;
            _productPricingService = productPricingService;
            _contentLocalizationService = contentLocalizationService;
            _entityRepository = entityRepository;
            _entityTypeRepository = entityTypeRepository;
        }
        public string GetToken(string laip)
        {
            
                sesion sesion = new sesion();
                string urlPath = "https://riews.reinfoempresa.com:8443";
                string request2 = urlPath + "/RIEWS/webapi/PublicServices/authenticationDefaultW";
                string SecretKey = "eyJhbGciOiJIUzUxMiJ9.eyJzdWIiOiJSZWN1cnNvcyBJbmZvcm3DoXRpY29zIEVtcHJlc2FyaWFsZXMsIFMuTC4iLCJpYXQiOjE1Nzc5NTgzNjYsImV4cCI6MTYwOTU4MDc2NywibmJmIjoxNTc3OTU4MzY2LCJpc3MiOiJhZHJpYW4iLCJlbnQiOiI0IiwiaW5zIjoiMSIsInVzYyI6IjMiLCJzbnUiOiI5OTk5LTg4ODgtMTEiLCJpcHMiOiJOVEV1TnpjdU1UTTNMakU0Tnc9PSIsImV4ZSI6IjExLjAuNSsxMC1wb3N0LVVidW50dS0wdWJ1bnR1MS4xMTguMDQiLCJsYW4iOiJzcGEiLCJqdGkiOiIwNTY0YzlmZS02NGI2LTRiNzAtYWZiZS04YmZhMDk1Y2U3NjkifQ.kkjBAlvDdNQrWC_8DCp5pEbMDBdHSBpRsmEZEKUm16bwn_45cktl3eudhOp7OxptqwgAt19prQowdKL3W3Zenw";
                WebRequest webRequest = WebRequest.Create(request2);
                //definimos el tipo de webrequest al que llamaremos
                webRequest.Method = "POST";
                //definimos content
                webRequest.ContentType = "application/json; charset=utf-8";
                //cargamos los datos a enviar
                using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
                {
                    string json = "{\"tokensite\":\"" + SecretKey/*System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes()) */+ "\"" +
                        ",\"ipbase64\":\"" + laip + "\",\"language\":\"SPA\"}";
                    streamWriter.Write(json);
                }
                var httpResponse = (HttpWebResponse)webRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var resultado = streamReader.ReadToEnd();
                    sesion = JsonConvert.DeserializeObject<sesion>(resultado);
                }
            return sesion.activeToken;
        }



        public string GetSession()
        {
            string se = HttpContext.Session.GetString("id");
            if (se == null)
            {
                se = GetToken(GetIP());
                HttpContext.Session.Set("id", System.Text.Encoding.UTF8.GetBytes(se));
            }
            return HttpContext.Session.GetString("id");
        }
        public string GetIP()
        {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString()));
        }

        public IViewComponentResult Invoke(WidgetInstanceViewModel widgetInstance)
        {

            //cambiamos aqui, tenemos que anaizar de donde vienen los widgets para poder diferenciar que cargar y donde.
            // se definene en la parte de admnistracion
            var productos = RecArticle(GetIP(),GetSession());
            //var model = new ProductWidgetComponentVm
            //{
            //    Id = widgetInstance.Id,
            //    WidgetName = widgetInstance.Name,
            //    Setting = JsonConvert.DeserializeObject<ProductWidgetSetting>(widgetInstance.Data)
            //};



            var model = new ProductWidgetComponentVm
            {
                Id = widgetInstance.Id,
                WidgetName = widgetInstance.Name,
                Setting = JsonConvert.DeserializeObject<ProductWidgetSetting>(widgetInstance.Data)
            };
            model.Products = new List<ProductThumbnail>();

            //CODIGO ORIGEN
            //var query = _productRepository.Query()
            //  .Where(x => x.IsPublished && x.IsVisibleIndividually);

            //if (model.Setting.CategoryId.HasValue && model.Setting.CategoryId.Value > 0)
            //{
            //    query = query.Where(x => x.Categories.Any(c => c.CategoryId == model.Setting.CategoryId.Value));
            //}

            //if (model.Setting.FeaturedOnly)
            //{
            //    query = query.Where(x => x.IsFeatured);
            //}

            //model.Products = query
            //  .Include(x => x.ThumbnailImage)
            //  .OrderByDescending(x => x.CreatedOn)
            //  .Take(model.Setting.NumberOfProducts)
            //  .Select(x => ProductThumbnail.FromProduct(x)).ToList();
            //foreach (var product in model.Products)
            //{
            //    product.Name = _contentLocalizationService.GetLocalizedProperty(nameof(Product), product.Id, nameof(product.Name), product.Name);
            //    product.ThumbnailUrl = _mediaService.GetThumbnailUrl(product.ThumbnailImage);
            //    product.CalculatedProductPrice = _productPricingService.CalculateProductPrice(product);
            //}

            //FIN CODIGO ORIGEN
            foreach (producto p in productos.Result.result)
            {
                ViewModels.ProductThumbnail tm = new ProductThumbnail();
                tm.Id = long.Parse(p.identifier);
                tm.Name = p.description;
                tm.ThumbnailUrl = p.imagelarge;
                int r = 0;
                _ = int.TryParse(p.stocks, out r);
                tm.StockQuantity = r;
                decimal pr = 0;
                 pr = decimal.Parse(p.pricewithtax ); 
                tm.Price = pr;
                tm.ReviewsCount = int.Parse(p.likeothers);
                tm.IsAllowToOrder = true;
                tm.Slug =tm.Name.Replace(" ", "-");
                Core.Models.Media pti = new ProductThumbnail().ThumbnailImage;
                tm.ThumbnailUrl = _mediaService.GetThumbnailUrl(pti);
                tm.ThumbnailUrl = _mediaService.GetURL(p.imagelarge);

                //tm.CalculatedProductPrice(p);
                //tm.CalculatedProductPrice = _productPricingService.CalculateProductPrice((decimal.Parse(p.pricewithtax)));
                tm.CalculatedProductPrice = _productPricingService.CalculateProductPrice(tm);
                model.Products.Add(tm);
                //añadimos a la tabla slug si no existe
                var entity =  _entityRepository
                .Query()
                .Include(x => x.EntityType)
                .FirstOrDefault(x => x.Slug == tm.Slug);
                if (entity == null)
                {
                    Entity en = new Entity();
                    
                    en.EntityId = (long)tm.Id;
                    en.Name = tm.Name;
                    en.Slug = tm.Slug+"-"+tm.Id;
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

            return View(this.GetViewPath(), model);
        }
        public async Task<DataCollection<producto>> RecArticle(string laip, string _sesionToken)
        {
            var result = new DataCollection<producto>();

            //resultList _area;
            // string token = GetToken(laip).activeToken;
            //string de url principal
            string urlPath = "https://riews.reinfoempresa.com:8443";
            string order = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(""));
            string init = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("1"));
            string max = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("9"));
            //string de la url del método de llamada
            //https://riews.reinfoempresa.com:8443/RIEWS/webapi/PrivateServices/articles1
            string request2 = urlPath + "/RIEWS/webapi/PrivateServices/articles5W";
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
                string json = "{\"token\":\"" + _sesionToken + "\",\"ipbase64\":\"" + laip + "\",\"orderby\":\"" + order + "\",\"initrec\":\"" + 1 + "\",\"maxrecs\":\"" + 9 + "\"}";
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
