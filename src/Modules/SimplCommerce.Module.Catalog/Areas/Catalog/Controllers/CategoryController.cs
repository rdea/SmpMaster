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
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.Catalog.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CategoryController : Controller
    {
        private int _pageSize;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IMediaService _mediaService;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Brand> _brandRepository;
        private readonly IProductPricingService _productPricingService;
        private readonly IContentLocalizationService _contentLocalizationService;

        public CategoryController(IRepository<Product> productRepository,
            IMediaService mediaService,
            IRepository<Category> categoryRepository,
            IRepository<Brand> brandRepository,
            IProductPricingService productPricingService,
            IContentLocalizationService contentLocalizationService,
            IConfiguration config)
        {
            _productRepository = productRepository;
            _mediaService = mediaService;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _productPricingService = productPricingService;
            _contentLocalizationService = contentLocalizationService;
            _pageSize = config.GetValue<int>("Catalog.ProductPageSize");
        }

        public IActionResult CategoryDetail(long id, SearchOption searchOption)
        {
            var category = _categoryRepository.Query().FirstOrDefault(x => x.Id == id);
            if (category == null)
            {
                return Redirect("~/Error/FindNotFound");
            }

            var model = new ProductsByCategory
            {
                CategoryId = category.Id,
                ParentCategorId = category.ParentId,
                CategoryName = _contentLocalizationService.GetLocalizedProperty(category, nameof(category.Name), category.Name),
                CategorySlug = category.Slug,
                CategoryMetaTitle = category.MetaTitle,
                CategoryMetaKeywords = category.MetaKeywords,
                CategoryMetaDescription = category.MetaDescription,
                CurrentSearchOption = searchOption,
                FilterOption = new FilterOption()
            };

            var query = _productRepository
                .Query()
                .Where(x => x.Categories.Any(c => c.CategoryId == category.Id) && x.IsPublished && x.IsVisibleIndividually);

            if (query.Count() == 0)
            {
                model.TotalProduct = 0;
                return View(model);
            }

            AppendFilterOptionsToModel(model, query);

            if (searchOption.MinPrice.HasValue)
            {
                query = query.Where(x => x.Price >= searchOption.MinPrice.Value);
            }

            if (searchOption.MaxPrice.HasValue)
            {
                query = query.Where(x => x.Price <= searchOption.MaxPrice.Value);
            }

            var brands = searchOption.GetBrands().ToArray();
            if (brands.Any())
            {
                query = query.Where(x => x.BrandId != null && brands.Contains(x.Brand.Slug));
            }

            model.TotalProduct = query.Count();
            var currentPageNum = searchOption.Page <= 0 ? 1 : searchOption.Page;
            var offset = (_pageSize * currentPageNum) - _pageSize;
            while (currentPageNum > 1 && offset >= model.TotalProduct)
            {
                currentPageNum--;
                offset = (_pageSize * currentPageNum) - _pageSize;
            }

            query = ApplySort(searchOption, query);

            var products = query
                .Include(x => x.ThumbnailImage)
                .Skip(offset)
                .Take(_pageSize)
                .Select(x => ProductThumbnail.FromProduct(x))
                .ToList();

            foreach (var product in products)
            {
                product.Name = _contentLocalizationService.GetLocalizedProperty(nameof(Product), product.Id, nameof(product.Name), product.Name);
                product.ThumbnailUrl = _mediaService.GetThumbnailUrl(product.ThumbnailImage);
                product.CalculatedProductPrice = _productPricingService.CalculateProductPrice(product);
            }

            model.Products = products;
            model.CurrentSearchOption.PageSize = _pageSize;
            model.CurrentSearchOption.Page = currentPageNum;

            return View(model);
        }

        private static IQueryable<Product> ApplySort(SearchOption searchOption, IQueryable<Product> query)
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

        private static void AppendFilterOptionsToModel(ProductsByCategory model, IQueryable<Product> query)
        {
            model.FilterOption.Price.MaxPrice = query.Max(x => x.Price);
            model.FilterOption.Price.MinPrice = query.Min(x => x.Price);

            model.FilterOption.Brands = query.Include(x => x.Brand)
                .Where(x => x.BrandId != null).ToList()
                .GroupBy(x => x.Brand)
                .Select(g => new FilterBrand
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    Slug = g.Key.Slug,
                    Count = g.Count()
                }).ToList();
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

            // filtro de division y seccion
            //'{"field":"DIVISION","operator":"=","fieldliteral":"'. (string)$Division. '","type":"numeric","connector":"and"},'.
            //                        '{"field":"SECTION","operator":"=","fieldliteral":"'. (string)$Section. '","type":"numeric","connector":';



            string statement = @"[{""statementv1"":[{""field"":""DESCRIPTION"",""operator"":""like"",""fieldliteral"":""%";
            statement += cadena;
            statement += @"%"",""type"":""text"",""connector"":""""}]}]";

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
                string json = "{\"token\":\"" + _sesionToken + "\",\"filter\":\"" + statement + "\",\"orderby\":\"" + order + "\",\"initrec\":\"" + 1 + "\",\"maxrecs\":\"" + 9 + "\",\"ipbase64\":\"" + laip + "\"}";
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
            string se = HttpContext.Session.GetString("id");
            if (se == null)
            {
                se = comunes.GetToken(GetIP());
                HttpContext.Session.Set("id", System.Text.Encoding.UTF8.GetBytes(se));
            }
            return HttpContext.Session.GetString("id");
        }

    }
}
