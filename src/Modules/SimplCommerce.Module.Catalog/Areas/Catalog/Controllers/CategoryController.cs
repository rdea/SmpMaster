using System.Collections.Generic;
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
        private readonly IRepository<Entity> _entityRepository;
        private readonly IRepositoryWithTypedId<EntityType, string> _entityTypeRepository;

        public CategoryController(IRepository<Product> productRepository,
            IMediaService mediaService,
            IRepository<Category> categoryRepository,
            IRepository<Brand> brandRepository,
            IProductPricingService productPricingService,
            IContentLocalizationService contentLocalizationService,
            IConfiguration config, 
            IRepository<Entity> entityRepository,
            IRepositoryWithTypedId<EntityType, string> entityTypeRepository)
        {
            _productRepository = productRepository;
            _mediaService = mediaService;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
            _productPricingService = productPricingService;
            _contentLocalizationService = contentLocalizationService;
            _entityRepository = entityRepository;
            _entityTypeRepository = entityTypeRepository;
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
            //TODO
            //sacar el total de lo artículos para el model totalproduct
            //dependiendo de la pagina, tendremos que pasar el init y el pagesize (fin = init+pagesize)

            var productosCount = RecuperaArtículosCategoria(GetIP(), GetSession(), category, searchOption);
            //if (productos.result.Count == 0 || productos is null)
            if (productosCount.result.Count == 0)
            {
                model.TotalProduct = 0;
                return View(model);
            }

            model.TotalProduct = productosCount.result.Count;

            //preparamos el filtro de todos los productos que tenemos, sin filtrar.
            AppendFilterOptionsToModelModifier(model, productosCount);


            var currentPageNum = searchOption.Page <= 0 ? 1 : searchOption.Page;
            var offset = (_pageSize * currentPageNum) - _pageSize;

            while (currentPageNum > 1 && offset >= model.TotalProduct)
            {
                currentPageNum--;
                offset = (_pageSize * currentPageNum) - _pageSize;
            }
            DataCollection<producto> productos;
            //if (searchOption.Sort != null)
            //{
            //    productos = RecuperaArtículosCategoria(GetIP(), GetSession(), category, (currentPageNum-1) * _pageSize, _pageSize-1, searchOption);
            //}
            //else
            //{
            //    productos = RecuperaArtículosCategoria(GetIP(), GetSession(), category, (currentPageNum - 1) * _pageSize, _pageSize-1);
            //}
            productos = RecuperaArtículosCategoria(GetIP(), GetSession(), category, (currentPageNum - 1) * _pageSize, _pageSize - 1, searchOption);

            if (productos is null)
            {
                model.TotalProduct = 0;
                return View(model);
            }
            
            foreach (var p in productos.result)
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
                tm.Slug = tm.Name.Replace(" ", "-")+ "-" + tm.Id;
                Core.Models.Media pti = new ProductThumbnail().ThumbnailImage;
                tm.ThumbnailUrl = _mediaService.GetThumbnailUrl(pti);
                tm.ThumbnailUrl = _mediaService.GetURL(p.imagelarge);
                tm.RatingAverage = double.Parse(p.likeothers);
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
                    en.Slug = tm.Slug;// + "-" + tm.Id;
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
            //var model = new SearchResult
            //{
            //    CurrentSearchOption = searchOption,
            //    FilterOption = new FilterOption()
            //};

            //if (productos.result.Count == 0 || productos is null)
            //{
            //    model.TotalProduct = 0;
            //    return View(model);
            //}
            ////AppendFilterOptionsToModel(model, query);

            //model.TotalProduct = productos.result.Count();
            //var currentPageNum = searchOption.Page <= 0 ? 1 : searchOption.Page;
            //var offset = (_pageSize * currentPageNum) - _pageSize;
            //while (currentPageNum > 1 && offset >= model.TotalProduct)
            //{
            //    currentPageNum--;
            //    offset = (_pageSize * currentPageNum) - _pageSize;
            //}


            //foreach (var p in productos.result)
            //{
            //    ProductThumbnail tm = new ProductThumbnail();
            //    tm.Id = long.Parse(p.identifier);
            //    tm.Name = p.description;
            //    tm.ThumbnailUrl = p.imagelarge;
            //    int r = 0;
            //    _ = int.TryParse(p.stocks, out r);
            //    tm.StockQuantity = r;
            //    decimal pr = 0;
            //    pr = decimal.Parse(p.pricewithtax);
            //    tm.Price = pr;
            //    tm.ReviewsCount = int.Parse(p.likeothers);
            //    tm.IsAllowToOrder = true;
            //    tm.Slug = tm.Name.Replace(" ", "-");
            //    Core.Models.Media pti = new ProductThumbnail().ThumbnailImage;
            //    tm.ThumbnailUrl = _mediaService.GetThumbnailUrl(pti);
            //    tm.ThumbnailUrl = _mediaService.GetURL(p.imagelarge);

            //    //tm.CalculatedProductPrice(p);
            //    //tm.CalculatedProductPrice = _productPricingService.CalculateProductPrice((decimal.Parse(p.pricewithtax)));
            //    tm.CalculatedProductPrice = _productPricingService.CalculateProductPrice(tm);
            //    model.Products.Add(tm);
            //    //añadimos a la tabla slug si no existe
            //    var entity = _entityRepository
            //    .Query()
            //    .Include(x => x.EntityType)
            //    .FirstOrDefault(x => x.Slug == tm.Slug);
            //    if (entity == null)
            //    {
            //        Entity en = new Entity();

            //        en.EntityId = (long)tm.Id;
            //        en.Name = tm.Name;
            //        en.Slug = tm.Slug + "-" + tm.Id;
            //        var enType = _entityTypeRepository.Query().FirstOrDefault(x => x.Id == "Product");
            //        en.EntityType = enType;

            //        //en.EntityType = (EntityType)enType;
            //        //en.EntityType = new EntityType("Product");
            //        //en.EntityType.AreaName = "Catalog";
            //        //en.EntityType.IsMenuable = false;
            //        //en.EntityType.RoutingController = "Product";
            //        //en.EntityType.RoutingAction = "ProductDetail";
            //        _entityRepository.Add(en);
            //        _entityRepository.SaveChanges();
            //    }
            //}


            //model.CurrentSearchOption.PageSize = _pageSize;
            //model.CurrentSearchOption.Page = currentPageNum;

            //return View(model);


            ////codigo origen
            //var category = _categoryRepository.Query().FirstOrDefault(x => x.Id == id);
            //if (category == null)
            //{
            //    return Redirect("~/Error/FindNotFound");
            //}

            //var model = new ProductsByCategory
            //{
            //    CategoryId = category.Id,
            //    ParentCategorId = category.ParentId,
            //    CategoryName = _contentLocalizationService.GetLocalizedProperty(category, nameof(category.Name), category.Name),
            //    CategorySlug = category.Slug,
            //    CategoryMetaTitle = category.MetaTitle,
            //    CategoryMetaKeywords = category.MetaKeywords,
            //    CategoryMetaDescription = category.MetaDescription,
            //    CurrentSearchOption = searchOption,
            //    FilterOption = new FilterOption()
            //};

            //var query = _productRepository
            //    .Query()
            //    .Where(x => x.Categories.Any(c => c.CategoryId == category.Id) && x.IsPublished && x.IsVisibleIndividually);

            //if (query.Count() == 0)
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

            //var brands = searchOption.GetBrands().ToArray();
            //if (brands.Any())
            //{
            //    query = query.Where(x => x.BrandId != null && brands.Contains(x.Brand.Slug));
            //}

            //model.TotalProduct = query.Count();
            //var currentPageNum = searchOption.Page <= 0 ? 1 : searchOption.Page;
            //var offset = (_pageSize * currentPageNum) - _pageSize;
            //while (currentPageNum > 1 && offset >= model.TotalProduct)
            //{
            //    currentPageNum--;
            //    offset = (_pageSize * currentPageNum) - _pageSize;
            //}

            //query = ApplySort(searchOption, query);

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
        private static void AppendFilterOptionsToModelModifier(ProductsByCategory model, DataCollection<producto> pr)
        {
            decimal priceMin = decimal.Zero;
            decimal priceMax = decimal.Zero;
            List<FilterBrand> fb = new List<FilterBrand>();
            foreach (var p in pr.result)
            {
                if (priceMin > decimal.Parse(p.pricewithtax.Replace(".", ",")))
                    priceMin = decimal.Parse(p.pricewithtax.Replace(".", ","));
                if (priceMax < decimal.Parse(p.pricewithtax.Replace(".", ",")))
                    priceMax = decimal.Parse(p.pricewithtax.Replace(".", ","));
                var filtr = fb.Find(x => x.Id == long.Parse(p.brand));

                if (filtr != null)
                {
                    filtr.Count++;
                }
                else {

                    FilterBrand f = new FilterBrand
                    {
                        Id = long.Parse(p.brand),
                        Name = p.brand,
                        Slug = p.brand,
                        Count = 1
                    };

//                    var entity = _entityRepository
//.Query()
//.Include(x => x.EntityType)
//.FirstOrDefault(x => x.Slug == f.Slug);

//                    if (entity == null)
//                    {
//                        Entity en = new Entity();

//                        en.EntityId = (long)p.brand
//                        en.Name = p.brand;
//                        en.Slug = f.Slug;// + "-" + tm.Id;
//                        var enType = _entityTypeRepository.Query().FirstOrDefault(x => x.Id == "Brand");
//                        en.EntityType = enType;

//                        //en.EntityType = (EntityType)enType;
//                        //en.EntityType = new EntityType("Product");
//                        //en.EntityType.AreaName = "Catalog";
//                        //en.EntityType.IsMenuable = false;
//                        //en.EntityType.RoutingController = "Product";
//                        //en.EntityType.RoutingAction = "ProductDetail";
//                        _entityRepository.Add(en);
//                        _entityRepository.SaveChanges();
//                    }

                    fb.Add(f);
                }
            }
            model.FilterOption.Price.MaxPrice = priceMax;
            model.FilterOption.Price.MinPrice = priceMin;

            model.FilterOption.Brands = fb;

            //model.FilterOption.Price.MaxPrice = query.Max(x => x.Price);
            //model.FilterOption.Price.MinPrice = query.Min(x => x.Price);

            //model.FilterOption.Brands = query.Include(x => x.Brand)
            //    .Where(x => x.BrandId != null).ToList()
            //    .GroupBy(x => x.Brand)
            //    .Select(g => new FilterBrand
            //    {
            //        Id = g.Key.Id,
            //        Name = g.Key.Name,
            //        Slug = g.Key.Slug,
            //        Count = g.Count()
            //    }).ToList();
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
        public DataCollection<producto> RecuperaArtículosCategoria(string laip, string _sesionToken, Category categoria, SearchOption soption)
        {
            var result = new DataCollection<producto>();

            //resultList _area;
            // string token = GetToken(laip).activeToken;
            //string de url principal
            string urlPath = "https://riews.reinfoempresa.com:8443";
            string order = System.Convert.ToBase64String(System.Text.Encoding.Default.GetBytes("DESCRIPTION ASC"));

            //esta sentencia funciona
            //[{"statementv1":[{"field":"DESCRIPTION","operator":"like","fieldliteral":"%%","type":"text","connector":"and"},{"field":"PRICEWITHTAX","operator":">=","fieldliteral":"'0.01'","type":"numeric","connector":"and"},{"field":"PRICEWITHTAX","operator":"<=","fieldliteral":"'1000'","type":"numeric","connector":""}]}]

            // string codeidentifier = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(identificador.ToString()));

            // filtro de division y seccion
            //'{"field":"DIVISION","operator":"=","fieldliteral":"'. (string)$Division. '","type":"numeric","connector":"and"},'.
            //                        '{"field":"SECTION","operator":"=","fieldliteral":"'. (string)$Section. '","type":"numeric","connector":';



            //string statement = @"[{""statementv1"":[{""field"":""DESCRIPTION"",""operator"":""like"",""fieldliteral"":""%";
            //statement += categoria.Name;
            //statement += @"%"",""type"":""text"",""connector"":""""}]}]";
            ///varios funcionaba
            //////string statement = @"[{""statementv1"":[{""field"":""DIVISION"",""operator"":""="",""fieldliteral"":""" + categoria.Division;
            //////if(categoria.Section > 0)
            //////{
            //////    statement += @""",""type"":""numeric"",""connector"":""and""},";
            //////    statement += @"{""field"":""SECTION"",""operator"":""="",""fieldliteral"":""" + categoria.Section;
            //////}
            //////if (soption.MinPrice != null)
            //////{
            //////    statement += @""",""type"":""numeric"",""connector"":""and""},";
            //////    statement += @"{""field"":""PRICEWITHTAX"",""operator"":"">="",""fieldliteral"":""" + soption.MinPrice;
            //////}
            //////if (soption.MaxPrice != null)
            //////{
            //////    statement += @""",""type"":""numeric"",""connector"":""and""},";
            //////    statement += @"{""field"":""PRICEWITHTAX"",""operator"":""<="",""fieldliteral"":""" + soption.MaxPrice;
            //////}
            //////if (soption.Brand != null)
            //////{
            //////    statement += @""",""type"":""numeric"",""connector"":""and""},";
            //////    statement += @"{""field"":""BRAND"",""operator"":""="",""fieldliteral"":""" + soption.Brand;
            //////}

            ////////statement  += @""",""type"":""numeric"",""connector"":""and""},";
            ////////statement += @"{""field"":""SUBSECTION"",""operator"":""="",""fieldliteral"":""" + categoria.Subsection;
            ////////statement += @""",""type"":""numeric"",""connector"":""and""},";
            ////////statement += @"{""field"":""FAMILY"",""operator"":""="",""fieldliteral"":""" + categoria.Family;
            ////////statement += @""",""type"":""numeric"",""connector"":""and""},";
            ////////statement += @"{""field"":""SUBFAMILY"",""operator"":""="",""fieldliteral"":""" + categoria.Subfamily;
            //////statement += @""",""type"":""numeric"",""connector"":""""}]}]";

            //string statement = @"[{""statementv1"":[{""field"":""DESCRIPTION"", ""operator"":""ilike"",""fieldliteral"":""%''%"",""type"":""text"",""connector"":""or""},";
            //statement +=@"{""field"":""DESCRIPTION2"",""operator"":""ilike"",""fieldliteral"":""%''%"",""type"":""text"",""connector"":""""}],""connector"":""AND""},";
           string  statement = @"[{""statementv1"":[{""field"":""DIVISION"",""operator"":""="",""fieldliteral"":""" + categoria.Division;
            if (categoria.Section > 0)
            {
                statement += @""",""type"":""numeric"",""connector"":""and""},";
                statement += @"{""field"":""SECTION"",""operator"":""="",""fieldliteral"":""" + categoria.Section;
            }
            if (soption.MinPrice != null)
            {
                statement += @""",""type"":""numeric"",""connector"":""and""},";
                statement += @"{""field"":""PRICEWITHTAX"",""operator"":"">="",""fieldliteral"":""" + soption.MinPrice;
            }
            if (soption.MaxPrice != null)
            {
                statement += @""",""type"":""numeric"",""connector"":""and""},";
                statement += @"{""field"":""PRICEWITHTAX"",""operator"":""<="",""fieldliteral"":""" + soption.MaxPrice;
            }
            if (soption.Brand != null)
            {
                statement += @""",""type"":""numeric"",""connector"":""and""},";
                statement += @"{""field"":""BRAND"",""operator"":""="",""fieldliteral"":""" + soption.Brand;
            }

            //statement  += @""",""type"":""numeric"",""connector"":""and""},";
            //statement += @"{""field"":""SUBSECTION"",""operator"":""="",""fieldliteral"":""" + categoria.Subsection;
            //statement += @""",""type"":""numeric"",""connector"":""and""},";
            //statement += @"{""field"":""FAMILY"",""operator"":""="",""fieldliteral"":""" + categoria.Family;
            //statement += @""",""type"":""numeric"",""connector"":""and""},";
            //statement += @"{""field"":""SUBFAMILY"",""operator"":""="",""fieldliteral"":""" + categoria.Subfamily;
            //      statement += @""",""type"":""numeric"",""connector"":""""}],""connector"":""""}]";
                  statement += @""",""type"":""numeric"",""connector"":""""}]}]";

            statement = @"[{""statementv1"":[{""field"":""DIVISION"",""operator"":""="",""fieldliteral"":""1"",""type"":""numeric"",""connector"":""}][{""statementv1"":[{""field"":""DESCRIPTION"",""operator"":""ilike"",""fieldliteral"":""%''%"",""type"":""text"",""connector"":""or""},{""field"":""DESCRIPTION2"",""operator"":""ilike"",""fieldliteral"":""%''%"",""type"":""text"",""connector"":""}],""connector"":""AND""},{""statementv1"":[{""field"":""PRICEWITHTAX"",""operator"":"">="",""fieldliteral"":""0"",""type"":""numeric"",""connector"":""and""},{""field"":""PRICEWITHTAX"",""operator"":""<="",""fieldliteral"":""99999"", ""type"":""numeric"",""connector"":""}],""connector"":""}]";




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
                string json = "{\"token\":\"" + _sesionToken + "\",\"filter\":\"" + statement + "\",\"orderby\":\"" + order + "\",\"initrec\":\"" + 1 + "\",\"maxrecs\":\"" + 50 + "\",\"ipbase64\":\"" + laip + "\"}";
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
        public int RecuperaCountArtículosCategoria(string laip, string _sesionToken, Category categoria, SearchOption soption)
        {
            var result = new DataCollection<producto>();

            //resultList _area;
            // string token = GetToken(laip).activeToken;
            //string de url principal
            string urlPath = "https://riews.reinfoempresa.com:8443";
            string order = System.Convert.ToBase64String(System.Text.Encoding.Default.GetBytes("DESCRIPTION ASC"));

            //esta sentencia funciona
            //[{"statementv1":[{"field":"DESCRIPTION","operator":"like","fieldliteral":"%%","type":"text","connector":"and"},{"field":"PRICEWITHTAX","operator":">=","fieldliteral":"'0.01'","type":"numeric","connector":"and"},{"field":"PRICEWITHTAX","operator":"<=","fieldliteral":"'1000'","type":"numeric","connector":""}]}]

            // string codeidentifier = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(identificador.ToString()));

            // filtro de division y seccion
            //'{"field":"DIVISION","operator":"=","fieldliteral":"'. (string)$Division. '","type":"numeric","connector":"and"},'.
            //                        '{"field":"SECTION","operator":"=","fieldliteral":"'. (string)$Section. '","type":"numeric","connector":';



            //string statement = @"[{""statementv1"":[{""field"":""DESCRIPTION"",""operator"":""like"",""fieldliteral"":""%";
            //statement += categoria.Name;
            //statement += @"%"",""type"":""text"",""connector"":""""}]}]";

            string statement = @"[{""statementv1"":[{""field"":""DIVISION"",""operator"":""="",""fieldliteral"":""" + categoria.Division;
            if (categoria.Section > 0)
            {
                statement += @""",""type"":""numeric"",""connector"":""and""},";
                statement += @"{""field"":""SECTION"",""operator"":""="",""fieldliteral"":""" + categoria.Section;
            }
            if (soption.MinPrice != null)
            {
                statement += @""",""type"":""numeric"",""connector"":""and""},";
                statement += @"{""field"":""PRICEWITHTAX"",""operator"":"">="",""fieldliteral"":""" + soption.MinPrice;
            }
            if (soption.MaxPrice != null)
            {
                statement += @""",""type"":""numeric"",""connector"":""and""},";
                statement += @"{""field"":""PRICEWITHTAX"",""operator"":""<="",""fieldliteral"":""" + soption.MaxPrice;
            }
            if (soption.Brand != null)
            {
                statement += @""",""type"":""numeric"",""connector"":""and""},";
                statement += @"{""field"":""BRAND"",""operator"":""="",""fieldliteral"":""" + soption.Brand;
            }

            //statement  += @""",""type"":""numeric"",""connector"":""and""},";
            //statement += @"{""field"":""SUBSECTION"",""operator"":""="",""fieldliteral"":""" + categoria.Subsection;
            //statement += @""",""type"":""numeric"",""connector"":""and""},";
            //statement += @"{""field"":""FAMILY"",""operator"":""="",""fieldliteral"":""" + categoria.Family;
            //statement += @""",""type"":""numeric"",""connector"":""and""},";
            //statement += @"{""field"":""SUBFAMILY"",""operator"":""="",""fieldliteral"":""" + categoria.Subfamily;
            statement += @""",""type"":""numeric"",""connector"":""""}]}]";
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
                string json = "{\"token\":\"" + _sesionToken + "\",\"filter\":\"" + statement + "\",\"orderby\":\"" + order + "\",\"initrec\":\"" + 1 + "\",\"maxrecs\":\"" + 50 + "\",\"ipbase64\":\"" + laip + "\"}";
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

            return result.result.Count;
        }

        public DataCollection<producto> RecuperaArtículosCategoria(string laip, string _sesionToken, Category categoria, int init, int size, SearchOption soption)
        {
            var result = new DataCollection<producto>();

            //resultList _area;
            // string token = GetToken(laip).activeToken;
            //string de url principal
            string urlPath = "https://riews.reinfoempresa.com:8443";
            string order;
            if (soption.Sort == null) {
                order = System.Convert.ToBase64String(System.Text.Encoding.Default.GetBytes("DESCRIPTION ASC")); 
            }
            else
            {
                order = System.Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(soption.Sort));
            }



            //esta sentencia funciona
            //[{"statementv1":[{"field":"DESCRIPTION","operator":"like","fieldliteral":"%%","type":"text","connector":"and"},{"field":"PRICEWITHTAX","operator":">=","fieldliteral":"'0.01'","type":"numeric","connector":"and"},{"field":"PRICEWITHTAX","operator":"<=","fieldliteral":"'1000'","type":"numeric","connector":""}]}]

            // string codeidentifier = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(identificador.ToString()));

            // filtro de division y seccion
            //'{"field":"DIVISION","operator":"=","fieldliteral":"'. (string)$Division. '","type":"numeric","connector":"and"},'.
            //                        '{"field":"SECTION","operator":"=","fieldliteral":"'. (string)$Section. '","type":"numeric","connector":';



            //string statement = @"[{""statementv1"":[{""field"":""DESCRIPTION"",""operator"":""like"",""fieldliteral"":""%";
            //statement += categoria.Name;
            //statement += @"%"",""type"":""text"",""connector"":""""}]}]";

            string statement = @"[{""statementv1"":[{""field"":""DIVISION"",""operator"":""="",""fieldliteral"":""" + categoria.Division;
            if (categoria.Section > 0)
            {
                statement += @""",""type"":""numeric"",""connector"":""and""},";
                statement += @"{""field"":""SECTION"",""operator"":""="",""fieldliteral"":""" + categoria.Section;
            }
            if (soption.MinPrice !=null)
            {
                statement += @""",""type"":""numeric"",""connector"":""and""},";
                statement += @"{""field"":""PRICEWITHTAX"",""operator"":"">="",""fieldliteral"":""" + soption.MinPrice;
            }
            if (soption.MaxPrice != null)
            {
                statement += @""",""type"":""numeric"",""connector"":""and""},";
                statement += @"{""field"":""PRICEWITHTAX"",""operator"":""<="",""fieldliteral"":""" + soption.MaxPrice;
            }
            if (soption.Brand != null)
            {
                statement += @""",""type"":""numeric"",""connector"":""and""},";
                statement += @"{""field"":""BRAND"",""operator"":""="",""fieldliteral"":""" + soption.Brand;
            }
            //statement  += @""",""type"":""numeric"",""connector"":""and""},";
            //statement += @"{""field"":""SUBSECTION"",""operator"":""="",""fieldliteral"":""" + categoria.Subsection;
            //statement += @""",""type"":""numeric"",""connector"":""and""},";
            //statement += @"{""field"":""FAMILY"",""operator"":""="",""fieldliteral"":""" + categoria.Family;
            //statement += @""",""type"":""numeric"",""connector"":""and""},";
            //statement += @"{""field"":""SUBFAMILY"",""operator"":""="",""fieldliteral"":""" + categoria.Subfamily;
            statement += @""",""type"":""numeric"",""connector"":""""}]}]";
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
                string json = "{\"token\":\"" + _sesionToken + "\",\"filter\":\"" + statement + "\",\"orderby\":\"" + order + "\",\"initrec\":\"" + init + "\",\"maxrecs\":\"" + (init+size) + "\",\"ipbase64\":\"" + laip + "\"}";
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

    }
}
