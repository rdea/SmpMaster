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
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Core.Services;


namespace SimplCommerce.Module.Catalog.Areas.Catalog.Components
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly IRepository<Category> _categoryRepository;
        private readonly IContentLocalizationService _contentLocalizationService; 
        private readonly IRepository<Entity> _entityRepository;
        private readonly IRepositoryWithTypedId<EntityType, string> _entityTypeRepository;

        sesion _sesion;
        public CategoryMenuViewComponent(IRepository<Category> categoryRepository, IContentLocalizationService contentLocalizationService, IRepository<Entity> entityRepository, IRepositoryWithTypedId<EntityType, string> entityTypeRepository)
        {
            _categoryRepository = categoryRepository;
            _contentLocalizationService = contentLocalizationService;
            _entityRepository = entityRepository;
            _entityTypeRepository = entityTypeRepository;
        }

        public IViewComponentResult Invoke()
        {

            //var categories = _categoryRepository.Query().Where(x => !x.IsDeleted && x.IncludeInMenu).ToList();

            //var categoryMenuItems = new List<CategoryMenuItem>();

            //var topCategories = categories.Where(x => !x.ParentId.HasValue).OrderByDescending(x => x.DisplayOrder);

            //foreach (var category in topCategories)
            //{
            //    var categoryMenuItem = Map(category);
            //    categoryMenuItems.Add(categoryMenuItem);
            //}
            //return View(this.GetViewPath(), categoryMenuItems);

           // cambios

            DataCollection<area> a = new DataCollection<area>();

            string ip = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString()));
            _sesion = GetToken(ip, _sesion);
            if (_sesion != null)
            {
                HttpContext.Session.Set("id", System.Text.Encoding.UTF8.GetBytes(_sesion.activeToken));

            }
            ISession sesion1 = HttpContext.Session;

            if (_sesion != null && _sesion.explained.ToString() != "Session NOT authorized")
            {
                a = areas(ip, sesion1.GetString("id")).Result;
            }

            var categoryMenuItems2 = new List<CategoryMenuItem>();
            foreach (area ar in a.result)
            {
                var cmi = Map(ar);
                categoryMenuItems2.Add(cmi);
            }
            return View(this.GetViewPath(), categoryMenuItems2);

         //   fin cambios


        }
        private CategoryMenuItem Map(area category)
        {
            var categoryMenuItem = new CategoryMenuItem
            {
                Id = long.Parse(category.division),
                Name = category.areaname
            };
            
            var childCategories = category.hijos;

            foreach (var childCategory in childCategories.OrderByDescending(x => x.areaname))
            {
                var childCategoryMenuItem = Map(childCategory);
                categoryMenuItem.AddChildItem(childCategoryMenuItem);
            }

            return categoryMenuItem;
        }

        private CategoryMenuItem Map(Category category)
        {
            var categoryMenuItem = new CategoryMenuItem
            {
                Id = category.Id,
                Name = _contentLocalizationService.GetLocalizedProperty(category, nameof(category.Name), category.Name),
                Slug = category.Slug
            };

            var childCategories = category.Children;
            foreach (var childCategory in childCategories.OrderByDescending(x => x.DisplayOrder))
            {
                var childCategoryMenuItem = Map(childCategory);
                categoryMenuItem.AddChildItem(childCategoryMenuItem);
            }

            return categoryMenuItem;
        }
        public sesion GetToken(string laip, sesion _sesion)
        {
            if (_sesion == null)
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
                _sesion = sesion;

            }
            return _sesion;
        }

        public async Task<DataCollection<area>> areas(string laip, string _sesion)
        {
            var result = new DataCollection<area>();

            //resultList _area;
            //string token = GetToken(laip).activeToken;
            //string de url principal
            string urlPath = "https://riews.reinfoempresa.com:8443";

            //string de la url del método de llamada
            string request2 = urlPath + "/RIEWS/webapi/PublicServices/areasW";
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
                string json = "{\"token\":\"" + _sesion + "\",\"ipbase64\":\"" + laip + "\"}";
                streamWriter.Write(json.ToString());
            }
            //obtenemos la respuesta del servidor
            var httpResponse = (HttpWebResponse)webRequest.GetResponse();
            //leemos la respuesta y la tratamos
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result2 = streamReader.ReadToEnd();
                //traducimos el resultado
                result = JsonConvert.DeserializeObject<DataCollection<area>>(result2);
            }
            List<area> AMenu = new List<area>();// = new IEnumerable<Area>();
            foreach (area a in result.result)
            {
                if ((int.Parse(a.division.ToString()) > 0 && int.Parse(a.section.ToString()) == 0 && int.Parse(a.subsection.ToString()) == 0) && int.Parse(a.family.ToString()) == 0 && int.Parse(a.subfamily.ToString()) == 0)
                {
                    AMenu.Add(a);
                }
                else
                {
                    if ((int.Parse(a.division.ToString()) > 0 && int.Parse(a.section.ToString()) > 0 && int.Parse(a.subsection.ToString()) == 0) && int.Parse(a.family.ToString()) == 0 && int.Parse(a.subfamily.ToString()) == 0)
                    {
                        AMenu.Add(a);
                    }
                }
                //añadimos a la tabla slug si no existe // 
                // TODO CAMBIOS
                Category c = new Category();
                c.Slug = a.areaname;
                c.Division = int.Parse(a.division);
                c.Name = a.areaname;
                c.Description = a.areaname;
                c.Family = int.Parse(a.family);
                c.Subfamily = int.Parse(a.subfamily);
                c.Subsection = int.Parse(a.subsection);
                c.Section = int.Parse(a.section);
                
                var entity = _entityRepository
                .Query()
                .Include(x => x.EntityType)
                .FirstOrDefault(x => x.Slug == c.Slug);
                if (entity == null)
                {
                    Entity en = new Entity();

                    en.EntityId = (long)c.Id;
                    en.Name = c.Description;
                    en.Slug = c.Slug + "-" + c.Id;
                    var enType = _entityTypeRepository.Query().FirstOrDefault(x => x.Id == "Category");
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
            List<area> AMenud = new List<area>();// = new IEnumerable<Area>();

            foreach (area a in AMenu)
            {
                if (int.Parse(a.division.ToString()) > 0 && int.Parse(a.section.ToString()) == 0)
                {
                    foreach(area a2 in AMenu)
                            {
                            if (int.Parse(a.division.ToString()) == int.Parse(a2.division.ToString()) && int.Parse(a2.section.ToString()) > 0)
                        {
                            a.hijos.Add(a2);
                        }
                        }
                    AMenud.Add(a);
                    
                                }
                            }





            result.result = AMenud;
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
