﻿using System;
using System.Linq;
using System.Net;
using System.Text;
using CRM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Catalog.Services;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.Search.Areas.Search.ViewModels;
using SimplCommerce.Module.Search.Models;

namespace SimplCommerce.Module.Search.Areas.Search.Controllers
{
    [Area("Search")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SearchController : Controller
    {
        private int _pageSize;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Brand> _brandRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IMediaService _mediaService;
        private readonly IRepository<Query> _queryRepository;
        private readonly IProductPricingService _productPricingService;
        private readonly IContentLocalizationService _contentLocalizationService;
        private readonly IRepository<Entity> _entityRepository;
        private readonly IRepositoryWithTypedId<EntityType, string> _entityTypeRepository;

        public SearchController(IRepository<Product> productRepository,
            IRepository<Brand> brandRepository,
            IRepository<Category> categoryRepository,
            IMediaService mediaService,
            IRepository<Query> queryRepository,
            IProductPricingService productPricingService,
            IContentLocalizationService contentLocalizationService,
            IRepository<Entity> entityRepository,
            IRepositoryWithTypedId<EntityType, string> entityTypeRepository,
            IConfiguration config)
        {
            _productRepository = productRepository;
            _brandRepository = brandRepository;
            _categoryRepository = categoryRepository;
            _mediaService = mediaService;
            _queryRepository = queryRepository;
            _productPricingService = productPricingService;
            _contentLocalizationService = contentLocalizationService;
            _pageSize = config.GetValue<int>("Catalog.ProductPageSize");
            _entityRepository = entityRepository;
            _entityTypeRepository = entityTypeRepository;
        }

        [HttpGet("search")]
        public IActionResult Index(SearchOption searchOption)
        {
            if (string.IsNullOrWhiteSpace(searchOption.Query))
            {
                return Redirect("~/");
            }
            var productos = RecuperaArtículosQuery(GetIP(), GetSession(), searchOption.Query);
            var model = new SearchResult
            {
                CurrentSearchOption = searchOption,
                FilterOption = new FilterOption()
            };

            if (productos.result.Count == 0 || productos is null)
            {
                model.TotalProduct = 0;
                return View(model);
            }
            //AppendFilterOptionsToModel(model, query);

            model.TotalProduct = productos.result.Count();
            var currentPageNum = searchOption.Page <= 0 ? 1 : searchOption.Page;
            var offset = (_pageSize * currentPageNum) - _pageSize;
            while (currentPageNum > 1 && offset >= model.TotalProduct)
            {
                currentPageNum--;
                offset = (_pageSize * currentPageNum) - _pageSize;
            }


            foreach (var p  in productos.result)
            {
                ProductThumbnail tm = new ProductThumbnail();
                tm.Id = long.Parse(p.identifier);
                tm.Name = p.description;
                tm.ThumbnailUrl = p.imagelarge;
                int r = 0;
                _ = int.TryParse(p.stocks, out r);
                tm.StockQuantity = r;
                decimal pr = 0;
                pr = decimal.Parse(p.pricewithtax.Replace(".",","));
                tm.Price = pr;
                tm.ReviewsCount = int.Parse(p.likeothers);
                tm.IsAllowToOrder = true;
                tm.Slug = tm.Name.Replace(" ", "-") + "-" + tm.Id;
                Core.Models.Media pti = new ProductThumbnail().ThumbnailImage;
                tm.ThumbnailUrl = _mediaService.GetThumbnailUrl(pti);
                tm.ThumbnailUrl = _mediaService.GetURL(p.imagelarge);

                //tm.CalculatedProductPrice(p);
                //tm.CalculatedProductPrice = _productPricingService.CalculateProductPrice((decimal.Parse(p.pricewithtax)));
                tm.CalculatedProductPrice = _productPricingService.CalculateProductPrice(tm);
                model.Products.Add(tm);
                //añadimos a la tabla slug si no existe
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

            
            model.CurrentSearchOption.PageSize = _pageSize;
            model.CurrentSearchOption.Page = currentPageNum;

            return View(model);







            //var brand = _brandRepository.Query().FirstOrDefault(x => x.Name == searchOption.Query && x.IsPublished);
            //if (brand != null)
            //{
            //    return Redirect(string.Format("~/{0}", brand.Slug));
            //}

            //var model = new SearchResult
            //{
            //    CurrentSearchOption = searchOption,
            //    FilterOption = new FilterOption()
            //};

            //var query = _productRepository.Query().Where(x => x.Name.Contains(searchOption.Query) && x.IsPublished && x.IsVisibleIndividually);
            //if (!query.Any())
            //{
            //    model.TotalProduct = 0;
            //    return View(model);
            //}




            //AppendFilterOptionsToModel(model, query);

            //if (searchOption.MinPrice.HasValue)
            //{
            //    query = query.Where(x => x.Price >= searchOption.MinPrice.Value);
            //}

            //if (searchOption.MaxPrice.HasValue)
            //{
            //    query = query.Where(x => x.Price <= searchOption.MaxPrice.Value);
            //}

            //if (string.Compare(model.CurrentSearchOption.Category, "all", StringComparison.OrdinalIgnoreCase) != 0)
            //{
            //    var categories = searchOption.GetCategories().ToArray();
            //    if (categories.Any())
            //    {
            //        query = query.Where(x => x.Categories.Any(c => categories.Contains(c.Category.Slug)));
            //    }
            //}

            //// EF Core bug, so we have to covert to Array
            //var brands = searchOption.GetBrands().ToArray();
            //if (brands.Any())
            //{
            //    query = query.Where(x => x.BrandId.HasValue && brands.Contains(x.Brand.Slug));
            //}

            //model.TotalProduct = query.Count();
            //var currentPageNum = searchOption.Page <= 0 ? 1 : searchOption.Page;
            //var offset = (_pageSize * currentPageNum) - _pageSize;
            //while (currentPageNum > 1 && offset >= model.TotalProduct)
            //{
            //    currentPageNum--;
            //    offset = (_pageSize * currentPageNum) - _pageSize;
            //}

            //SaveSearchQuery(searchOption, model);

            //query = AppySort(searchOption, query);

            //var products = query
            //    .Include(x => x.ThumbnailImage)
            //    .Skip(offset)
            //    .Take(_pageSize)
            //    .Select(x => ProductThumbnail.FromProduct(x))
            //    .ToList();

            //foreach (var product in products)
            //{
            //    product.Name = _contentLocalizationService.GetLocalizedProperty(nameof(Product), product.Id, nameof(product.Name), product.Name);
            //    product.ThumbnailUrl = _mediaService.GetThumbnailUrl(product.ThumbnailImage);
            //    product.CalculatedProductPrice = _productPricingService.CalculateProductPrice(product);
            //}

            //model.Products = products;
            //model.CurrentSearchOption.PageSize = _pageSize;
            //model.CurrentSearchOption.Page = currentPageNum;

            //return View(model);
        }
        public DataCollection<producto> RecuperaArtículosQuery(string laip, string _sesionToken, string cadena)
        {
            var result = new DataCollection<producto>();
            
            //resultList _area;
            // string token = GetToken(laip).activeToken;
            //string de url principal
            string urlPath = "https://riews.reinfoempresa.com:8443"; 
            string order = System.Convert.ToBase64String(System.Text.Encoding.Default.GetBytes("DESCRIPTION DESC"));

            //esta sentencia funciona
            //[{"statementv1":[{"field":"DESCRIPTION","operator":"like","fieldliteral":"%%","type":"text","connector":"and"},{"field":"PRICEWITHTAX","operator":">=","fieldliteral":"'0.01'","type":"numeric","connector":"and"},{"field":"PRICEWITHTAX","operator":"<=","fieldliteral":"'1000'","type":"numeric","connector":""}]}]

            // string codeidentifier = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(identificador.ToString()));
            //cnsulta origen antes funcionaba
            //string statement = @"[{""statementv1"":[{""field"":""DESCRIPTION"",""operator"":""ilike"",""fieldliteral"":""%";
            //statement += cadena;
            //statement += @"%"",""type"":""text"",""connector"":""""}]}]";

            string statement = @"[{""statementv1"":[{""field"":""DESCRIPTION"",""operator"":""ilike"",""fieldliteral"":""%";
            statement += cadena;
            statement += @"%"",""type"":""text"",""connector"":""""}],""connector"":""""}]";

            statement = System.Convert.ToBase64String(Encoding.Default.GetBytes(statement));
            //statement = @"W3sic3RhdGVtZW50djEiOlt7ImZpZWxkIjoiREVTQ1JJUFRJT04iLCJvcGVyYXRvciI6Imxpa2UiLCJmaWVsZGxpdGVyYWwiOiIlbWFydGklIiwidHlwZSI6InRleHQiLCJjb25uZWN0b3IiOiJhbmQifSx7ImZpZWxkIjoiUFJJQ0VXSVRIVEFYIiwib3BlcmF0b3IiOiI+PSIsImZpZWxkbGl0ZXJhbCI6IicwLjAxJyIsInR5cGUiOiJudW1lcmljIiwiY29ubmVjdG9yIjoiYW5kIn0seyJmaWVsZCI6IlBSSUNFV0lUSFRBWCIsIm9wZXJhdG9yIjoiPD0iLCJmaWVsZGxpdGVyYWwiOiInMTAwMCciLCJ0eXBlIjoibnVtZXJpYyIsImNvbm5lY3RvciI6IiJ9XX1d";
           // statement = @"W3sic3RhdGVtZW50djEiOlt7ImZpZWxkIjoiREVTQ1JJUFRJT04iLCJvcGVyYXRvciI6Imxpa2UiLCJmaWVsZGxpdGVyYWwiOiIlJSIsInR5cGUiOiJ0ZXh0IiwiY29ubmVjdG9yIjoiYW5kIn0seyJmaWVsZCI6IlBSSUNFV0lUSFRBWCIsIm9wZXJhdG9yIjoiPj0iLCJmaWVsZGxpdGVyYWwiOiInMC4wMSciLCJ0eXBlIjoibnVtZXJpYyIsImNvbm5lY3RvciI6ImFuZCJ9LHsiZmllbGQiOiJQUklDRVdJVEhUQVgiLCJvcGVyYXRvciI6Ijw9IiwiZmllbGRsaXRlcmFsIjoiJzEwMDAnIiwidHlwZSI6Im51bWVyaWMiLCJjb25uZWN0b3IiOiIifV19XQ==";
            //string de la url del método de llamada
            //https://riews.reinfoempresa.com:8443/RIEWS/webapi/PrivateServices/articles1
            string request2 = urlPath + "/RIEWS/webapi/PrivateServices/articles2W";
            //creamos un webRequest con el tipo de método.
            WebRequest webRequest = WebRequest.Create(request2);
            //definimos el tipo de webrequest al que llamaremos
            webRequest.Method = "POST";
            //definimos content\
            webRequest.ContentType = "application/json; charset=utf-8";
            //cargamos los datos a enviar
            using (var streamWriter = new System.IO.StreamWriter(webRequest.GetRequestStream()))
            {
                //string json = "{\"token\":\"" + token + "\",\"ipbase64\":\"" + laip +"}";
                string json = "{\"token\":\"" + _sesionToken + "\",\"filter\":\"" +statement + "\",\"orderby\":\"" + order + "\",\"initrec\":\"" + 1 + "\",\"maxrecs\":\"" + 9 + "\",\"ipbase64\":\"" + laip + "\"}";
                streamWriter.Write(json.ToString());
                //"  "
            }
            //obtenemos la respuesta del servidor
            var httpResponse = (HttpWebResponse)webRequest.GetResponse();
            //leemos la respuesta y la tratamos
            using (var streamReader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
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

        private static IQueryable<Product> AppySort(SearchOption searchOption, IQueryable<Product> query)
        {
            var sortBy = searchOption.Sort ?? string.Empty;
            switch (sortBy.ToLower())
            {
                case "price-desc":
                    query = query.OrderByDescending(x => x.Price);
                    break;
                default:
                    query = query.OrderBy(x => x.Price);
                    break;
            }

            return query;
        }

        private static void AppendFilterOptionsToModel(SearchResult model, IQueryable<Product> query)
        {
            model.FilterOption.Price.MaxPrice = query.Max(x => x.Price);
            model.FilterOption.Price.MinPrice = query.Min(x => x.Price);

            model.FilterOption.Categories = query
                .SelectMany(x => x.Categories)
                .GroupBy(x => new
                {
                    x.Category.Id,
                    x.Category.Name,
                    x.Category.Slug,
                    x.Category.ParentId
                })
                .Select(g => new FilterCategory
                {
                    Id = (int)g.Key.Id,
                    Name = g.Key.Name,
                    Slug = g.Key.Slug,
                    ParentId = g.Key.ParentId,
                    Count = g.Count()
                }).ToList();

            // TODO an EF Core bug, so we have to do evaluation in client
            model.FilterOption.Brands = query.Include(x => x.Brand)
               .Where(x => x.BrandId != null).ToList()
               .GroupBy(x => x.Brand)
               .Select(g => new FilterBrand
               {
                   Id = (int)g.Key.Id,
                   Name = g.Key.Name,
                   Slug = g.Key.Slug,
                   Count = g.Count()
               }).ToList();
        }

        private void SaveSearchQuery(SearchOption searchOption, SearchResult model)
        {
            var query = new Query
            {
                CreatedOn = DateTimeOffset.Now,
                QueryText = searchOption.Query,
                ResultsCount = model.TotalProduct
            };

            _queryRepository.Add(query);
            _queryRepository.SaveChanges();
        }
    }
}
