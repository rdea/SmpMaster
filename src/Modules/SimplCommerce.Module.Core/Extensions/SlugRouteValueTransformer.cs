using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Core.Models;

namespace SimplCommerce.Module.Core.Extensions
{
    public class SlugRouteValueTransformer : DynamicRouteValueTransformer
    {
        private readonly IRepository<Entity> _entityRepository;

        public SlugRouteValueTransformer(IRepository<Entity> entityRepository)
        {
            _entityRepository = entityRepository;
        }

        public override async ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            //codigo origen

            var requestPath = httpContext.Request.Path.Value;

            if (!string.IsNullOrEmpty(requestPath) && requestPath[0] == '/')
            {
                // Trim the leading slash
                requestPath = requestPath.Substring(1);
            }

            var entity = await _entityRepository
                .Query()
                .Include(x => x.EntityType)
                .FirstOrDefaultAsync(x => x.Slug == requestPath);

            if (entity == null)
            {
                return null;
            }

            return new RouteValueDictionary
            {
                { "area", entity.EntityType.AreaName },
                { "controller", entity.EntityType.RoutingController },
                { "action", entity.EntityType.RoutingAction },
                { "id", entity.EntityId }
            };



            // cambios de ruteo
            //var requestPath = httpContext.Request.Path.Value;
            //RouteValueDictionary rvd;
            //if (!string.IsNullOrEmpty(requestPath) && requestPath[0] == '/')
            //{

            //    int val = 0;
            //    val = requestPath.Length;
            //    var ruta = new string[val];
            //        ruta = requestPath.Substring(1).Split('-');
            //    string controller = string.Empty;
            //    string action = string.Empty;
            //    if (val>1 && ruta[1] == "p")
            //    {
            //        controller = "Product";
            //        action = "ProductDetail";
            //        rvd = new RouteValueDictionary
            //        {
            //    { "area", "Catalog" },
            //        { "controller", controller},
            //        { "action", action },
            //        { "id", ruta[0] }
            //    };
            //    }
            //    else if (val > 1 && ruta[1] == "c")
            //    {
            //        controller = "Category";
            //        action = "CategoryDetail";
            //        rvd = new RouteValueDictionary
            //        {
            //    { "area", "Catalog" },
            //        { "controller", controller},
            //        { "action", action },
            //        { "id", ruta[0] }
            //    };

            //    }
            //    else {

            //        // Trim the leading slash
            //        requestPath = requestPath.Substring(1);

            //        var entity = await _entityRepository
            //            .Query()
            //            .Include(x => x.EntityType)
            //            .FirstOrDefaultAsync(x => x.Slug == requestPath);

            //        if (entity == null)
            //        {
            //            return null;
            //        }

            //        rvd = new RouteValueDictionary
            //        {
            //            { "area", entity.EntityType.AreaName },
            //            { "controller", entity.EntityType.RoutingController },
            //            { "action", entity.EntityType.RoutingAction },
            //            { "id", entity.EntityId }
            //        };

            //    }

            //    return rvd;
            //}
            //else
            //{
            //    return null;
            //}

        }
    }
}
