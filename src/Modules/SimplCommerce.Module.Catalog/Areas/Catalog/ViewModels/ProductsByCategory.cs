using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SimplCommerce.Module.Catalog.Areas.Catalog.ViewModels
{
    public class ProductsByCategory
    {
        public long CategoryId { get; set; }

        public long? ParentCategorId { get; set; }

        public string CategoryName { get; set; }

        public string CategorySlug { get; set; }

        public string CategoryMetaTitle { get; set; }

        public string CategoryMetaKeywords { get; set; }

        public string CategoryMetaDescription { get; set; }

        public int TotalProduct { get; set; }

        public IList<ProductThumbnail> Products { get; set; } = new List<ProductThumbnail>();

        public FilterOption FilterOption { get; set; }

        public SearchOption CurrentSearchOption { get; set; }

        public IList<SelectListItem> AvailableSortOptions => new List<SelectListItem>
        {
                        new SelectListItem { Text = "Nombre: ascendente", Value = "DESCRIPTION ASC" },
                                    new SelectListItem { Text = "Nombre: Descendente", Value = "DESCRIPTION DESC" },
            new SelectListItem { Text = "Price: Low to High", Value = "PRICEWITHTAX ASC" },
            new SelectListItem { Text = "Price: High to Low", Value = "PRICEWITHTAX DESC" }
        };
    }
}
