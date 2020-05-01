using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CRM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;
using SimplCommerce.Module.ShoppingCart.Models;
using SimplCommerce.Module.ShoppingCart.Services;

namespace SimplCommerce.Module.ShoppingCart.Areas.ShoppingCart.Controllers
{
    [Area("ShoppingCart")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CartController : Controller
    {
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly ICartService _cartService;
        private readonly IMediaService _mediaService;
        private readonly IWorkContext _workContext;
        private readonly ICurrencyService _currencyService;

        public CartController(
            IRepository<CartItem> cartItemRepository,
            ICartService cartService,
            IMediaService mediaService,
            IWorkContext workContext,
            ICurrencyService currencyService)
        {
            _cartItemRepository = cartItemRepository;
            _cartService = cartService;
            _mediaService = mediaService;
            _workContext = workContext;
            _currencyService = currencyService;
        }

        [HttpPost("cart/add-item")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartModel model)
        {
            var currentUser = await _workContext.GetCurrentUser();
         //   var result = await _cartService.AddToCart(currentUser.Id, model.ProductId, model.Quantity);
            var result2 = AddLineaCarrito(GetIP(), GetSession(), model.ProductId, model.Quantity);
            if (result2.status == "OK")
            {
                return RedirectToAction("AddToCartResult", new { productId = model.ProductId });
            }
            else
            {
                return Ok(new { Error = true, Message = result2.explained });
            }
   //         function CRGDSPApigetBasketLine2($nIdentifier, $nCant, $nLong, $nWidth, $nThick) // add new basket line with data structure json 
   //         {   
   //$formData = array(
   //          "codeidentifier" 	=> (string)$nIdentifier,
   //          "cant" 	      	=> (string)$nCant,
   //          "long" 		=> (string)$nLong,
   //          "width" 		=> (string)$nWidth,
   //          "thick" 		=> (string)$nThick,
   //          "token" 		=> unserializeObj($_SESSION["_ObjSession"])->ActualToken,
   //          "ipbase64" 	=> $_SESSION["_IpAddressSession"]
   //         );
   //$response = getPromisePost($_SESSION["CRGlobalUrl"]. "/RIEWS/webapi/PrivateServices/basketline2W", $formData);
   //             if ($response->status == "OK")
   //   {
   //    $this->explained = $response->explained;
   //    $this->ActualLineNumber = parseInt($response->newlinenumber);
   //    $this->statusNewLine = 1;  //0-original, 1-live, 2-fault, 3-dont auctorization
   //         }
   //else
   //         {
   //    $this->explained = $response->explained;
   //             if ($response->status == "1001")
   //       {
	  // $this->statusNewLine = 2;  //0-original, 1-live, 2-fault, 3-dont auctorization	
   //             }
   //    else
   //             {
   //                 if ($response->status == "999")
   //           {
	  //     $this->statusNewLine = 3;  //0-original, 1-live, 2-fault, 3-dont auctorization
   //                 }
   //        else
   //                 {
	  //     $this->explained = "error response";
	  //     $this->statusNewLine = 2;  //0-original, 1-live, 2-fault, 3-dont auctorization
   //                 }
   //             }
   //         }
   //     }


    }

    [HttpGet("cart/add-item-result")]
        public async Task<IActionResult> AddToCartResult(long productId)
        {
            //recuperamos los datos de Previsualizacion del carrito.
            var carrito = RecuperaTotalCarrito(GetIP(), GetSession());
           // var currentUser = await _workContext.GetCurrentUser();
          //  var cart = await _cartService.GetActiveCartDetails(currentUser.Id);

            var model = new AddToCartResult(_currencyService)
            {
                CartItemCount = int.Parse(carrito.totallines),
                CartAmount = decimal.Parse(carrito.totaltopay.Replace(".", ","))
            };
            //tenemos que recuperar los datos de la ultima linea añadida
            var articulo = RecuperaArtículo(GetIP(), GetSession(), productId);
            model.ProductName = articulo.result.description;
            model.ProductImage = articulo.result.imagesmall;
            model.ProductPrice = decimal.Parse(articulo.result.pricewithtax.Replace(".", ","));
            //ponemos 1 pode defecto mientras no completamos el siguiente paso
            model.Quantity = 1;
            //leemos la cantidad de la ultima linea
            //model.Quantity = addedProduct.Quantity;
            // codigo origen
            //var addedProduct = cart.Items.First(x => x.ProductId == productId);
            //model.ProductName = addedProduct.ProductName;
            //model.ProductImage = addedProduct.ProductImage;
            //model.ProductPrice = addedProduct.ProductPrice;
            //model.Quantity = addedProduct.Quantity;

            return PartialView(model);
        }

        [HttpGet("cart")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("cart/list")]
        public async Task<IActionResult> List()
        {
            //aqui carga los datos de carrito   
            try
            {
                var carrito = DataCarrito();
                return Json(carrito);
            }
            catch (Exception e)
            {
                var currentUser = await _workContext.GetCurrentUser();
                var cart = await _cartService.GetActiveCartDetails(currentUser.Id);
                return Json(cart);
            }

        }

        [HttpPost("cart/update-item-quantity")]
        public async Task<IActionResult> UpdateQuantity([FromBody] CartQuantityUpdate model)
        {
            var currentUser = await _workContext.GetCurrentUser();
            var cart = await _cartService.GetActiveCart(currentUser.Id);

            if (cart == null)
            {
                return NotFound();
            }

            if (cart.LockedOnCheckout)
            {
                return CreateCartLockedResult();
            }

            var cartItem = _cartItemRepository.Query().Include(x => x.Product).FirstOrDefault(x => x.Id == model.CartItemId && x.Cart.CreatedById == currentUser.Id);
            if (cartItem == null)
            {
                return NotFound();
            }

            if(model.Quantity > cartItem.Quantity) // always allow user to descrease the quality
            {
                if (cartItem.Product.StockTrackingIsEnabled && cartItem.Product.StockQuantity < model.Quantity)
                {
                    return Ok(new { Error = true, Message = $"There are only {cartItem.Product.StockQuantity} items available for {cartItem.Product.Name}" });
                }
            }

            cartItem.Quantity = model.Quantity;
            _cartItemRepository.SaveChanges();

            return await List();
        }

        [HttpPost("cart/apply-coupon")]
        public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponForm model)
        {
            var currentUser = await _workContext.GetCurrentUser();
            var cart = await _cartService.GetActiveCart(currentUser.Id);
            if(cart == null)
            {
                return NotFound();
            }

            if (cart.LockedOnCheckout)
            {
                return CreateCartLockedResult();
            }

            var validationResult =  await _cartService.ApplyCoupon(cart.Id, model.CouponCode);
            if (validationResult.Succeeded)
            {
                var cartVm = await _cartService.GetActiveCartDetails(currentUser.Id);
                return Json(cartVm);
            }

            return Json(validationResult);
        }

        [HttpPost("cart/save-ordernote")]
        public async Task<IActionResult> SaveOrderNote([FromBody] SaveOrderNote model)
        {
            var currentUser = await _workContext.GetCurrentUser();
            var cart = await _cartService.GetActiveCart(currentUser.Id);
            if(cart == null)
            {
                return NotFound();
            }

            cart.OrderNote = model.OrderNote;
            await _cartItemRepository.SaveChangesAsync();
            return Accepted();
        }

        [HttpPost("cart/remove-item")]
        public async Task<IActionResult> Remove([FromBody] long itemId)
        {
            var currentUser = await _workContext.GetCurrentUser();
            var cart = await _cartService.GetActiveCart(currentUser.Id);
            if (cart == null)
            {
                return NotFound();
            }

            if (cart.LockedOnCheckout)
            {
                return CreateCartLockedResult();
            }

            var cartItem = _cartItemRepository.Query().FirstOrDefault(x => x.Id == itemId && x.Cart.CreatedById == currentUser.Id);
            if (cartItem == null)
            {
                return NotFound();
            }

            _cartItemRepository.Remove(cartItem);
            _cartItemRepository.SaveChanges();

            return await List();
        }

        private IActionResult CreateCartLockedResult()
        {
            return Ok(new { Error = true, Message = "Cart is locked for checkout. Please complete or cancel the checkout first" });
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
        public DataCollectionSingle<producto> AddLineaCarrito(string laip, string _sesionToken, long identificador, int cantidad)
        {
            var result = new DataCollectionSingle<producto>();

            //resultList _area;
            // string token = GetToken(laip).activeToken;
            //string de url principal
            string urlPath = "https://riews.reinfoempresa.com:8443";
            //string codeidentifier = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(identificador.ToString()));
            string codeidentifier = identificador.ToString();
            string _cantidad = cantidad.ToString();
            //string de la url del método de llamada
            //https://riews.reinfoempresa.com:8443/RIEWS/webapi/PrivateServices/articles1
            string request2 = urlPath + "/RIEWS/webapi/PrivateServices/basketline2W";
            //creamos un webRequest con el tipo de método.
            WebRequest webRequest = WebRequest.Create(request2);
            //definimos el tipo de webrequest al que llamaremos
            webRequest.Method = "POST";
            //definimos content\
            webRequest.ContentType = "application/json; charset=utf-8";
            //cargamos los datos a enviar
            using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
            {
                //$formData = array(
                //          "codeidentifier" 	=> (string)$nIdentifier,
                //          "cant" 	      	=> (string)$nCant,
                //          "long" 		=> (string)$nLong,
                //          "width" 		=> (string)$nWidth,
                //          "thick" 		=> (string)$nThick,
                //          "token" 		=> unserializeObj($_SESSION["_ObjSession"])->ActualToken,
                //          "ipbase64" 	=> $_SESSION["_IpAddressSession"]
                //         );

                string json = "{\"token\":\"" + _sesionToken + "\",\"ipbase64\":\"" + laip + "\",\"codeidentifier\":\"" + codeidentifier + "\",\"cant\":\""+_cantidad+"\"}";
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
                 result = JsonConvert.DeserializeObject<DataCollectionSingle<producto>>(result2);
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
        public DataCollectionSingle<producto> RecuperaArtículo(string laip, string _sesionToken, long identificador)
        {
            var result = new DataCollectionSingle<producto>();

            //resultList _area;
            // string token = GetToken(laip).activeToken;
            //string de url principal
            string urlPath = "https://riews.reinfoempresa.com:8443";
            //string codeidentifier = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(identificador.ToString()));
            string codeidentifier = identificador.ToString();

            //string de la url del método de llamada
            //https://riews.reinfoempresa.com:8443/RIEWS/webapi/PrivateServices/articles1
            string request2 = urlPath + "/RIEWS/webapi/PrivateServices/articles1W";
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
                result = JsonConvert.DeserializeObject<DataCollectionSingle<producto>>(result2);
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
        public BasketlinestotalRecibe RecuperaTotalCarrito(string laip, string _sesionToken)
        {
            var result = new BasketlinestotalRecibe();

            //string de url principal
            string urlPath = "https://riews.reinfoempresa.com:8443";

            //string de la url del método de llamada
            //https://riews.reinfoempresa.com:8443/RIEWS/webapi/PrivateServices/articles1
            string request2 = urlPath + "/RIEWS/webapi/PrivateServices/basketlinestotalW";
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
                string json = "{\"token\":\"" + _sesionToken + "\",\"ipbase64\":\"" + laip + "\"}";
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
                result = JsonConvert.DeserializeObject<BasketlinestotalRecibe>(result2);
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
        public void GetStructCarrito(string laip, string _sesionToken)
        {
            var result = new DataCollectionSingle<producto>();

            //string de url principal
            string urlPath = "https://riews.reinfoempresa.com:8443";

            //string de la url del método de llamada
            //https://riews.reinfoempresa.com:8443/RIEWS/webapi/PrivateServices/articles1
            string request2 = urlPath + "/RIEWS/webapi/PrivateServices/basketlineGetStructureW";
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
                string json = "{\"token\":\"" + _sesionToken + "\",\"ipbase64\":\"" + laip + "\"}";
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
                result = JsonConvert.DeserializeObject<DataCollectionSingle<producto>>(result2);
                //result = JsonConvert.DeserializeObject<BasketlinestotalRecibe>(result2);
            }
            //
            //if (_area == null)
            //{
            //    _area = new resultList();
            //    _area.result = new List<result>();
            //    _area.result[0].areaname = "vacia";
            //}

          
        }

        public DataCollectionSingle<producto> RecuperaLineasCarrito(string laip, string _sesionToken, string linea)
        {
            var result = new DataCollectionSingle<producto>();

            //string de url principal
            string urlPath = "https://riews.reinfoempresa.com:8443";

            //string de la url del método de llamada
            //https://riews.reinfoempresa.com:8443/RIEWS/webapi/PrivateServices/articles1
            string request2 = urlPath + "/RIEWS/webapi/PrivateServices/basketline1W";
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
                string json = "{\"token\":\"" + _sesionToken + "\",\"ipbase64\":\"" + laip + "\",\"line\":\""+linea+"\"}";
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
                result = JsonConvert.DeserializeObject<DataCollectionSingle<producto>>(result2);
                //result = JsonConvert.DeserializeObject<BasketlinestotalRecibe>(result2);
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
        public DataCollection<CartLine> RecuperatodasLineasCarrito(string laip, string _sesionToken)
        {
            var result = new DataCollection<CartLine>();

            //string de url principal
            string urlPath = "https://riews.reinfoempresa.com:8443";
            // string filtro = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("'@[{"field":"REFERENCIA","operator":" >= ","fieldliteral":"0","type":"text","connector":""}]'"));
            string statement = @"[{""statementv1"":[{""field"":""LINE"",""operator"":"">="",""fieldliteral"":""0"",""type"":""text"",""connector"":""""}]}]";
           // string statement = @"[]";
            statement = System.Convert.ToBase64String(Encoding.Default.GetBytes(statement));
            string order = System.Convert.ToBase64String(System.Text.Encoding.Default.GetBytes("DESCRIPTION DESC"));

            //string de la url del método de llamada
            //https://riews.reinfoempresa.com:8443/RIEWS/webapi/PrivateServices/articles1
            string request2 = urlPath + "/RIEWS/webapi/PrivateServices/basketlines5W";
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
                string json = "{\"token\":\"" + _sesionToken + "\",\"ipbase64\":\"" + laip + "\",\"orderby\":\"" + order + "\",\"initrec\":\"" + 1 + "\",\"maxrecs\":\"" + 9 + "\",\"filter\":\"" + statement + "\"}";
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
                result = JsonConvert.DeserializeObject<DataCollection<CartLine>>(result2);
                //result = JsonConvert.DeserializeObject<BasketlinestotalRecibe>(result2);
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

        public CartVm DataCarrito() {
            //GetStructCarrito(GetIP(), GetSession());
            var blc = RecuperaTotalCarrito(GetIP(), GetSession());
            var todas = RecuperatodasLineasCarrito(GetIP(), GetSession());
            Random r = new Random();
            
            // int totalLineas = int.Parse(blc.totallines);
            var cartVm = new CartVm(_currencyService)
            {
                Id = r.Next(0, 60000),//int.Parse(DateTime.Now.ToString()),
                IsProductPriceIncludeTax = true
                //TaxAmount = cart.TaxAmount,
                //ShippingAmount = cart.ShippingAmount,
                //OrderNote = cart.OrderNote
            };
            
            foreach (CartLine ln in todas.result)
            {
                CartItemVm civ = new CartItemVm(_currencyService);
                civ.Id = long.Parse(ln.line);
                civ.ProductId = long.Parse(ln.identifier);
                var arti = RecuperaArtículo(GetIP(), GetSession(), civ.ProductId);
                int sta=0;
                _ = int.TryParse(arti.result.stocks, out sta);
                civ.ProductStockQuantity = decimal.ToInt32(decimal.Parse(arti.result.stocks));
                civ.ProductName = ln.description;
                civ.ProductPrice = decimal.Parse(ln.pricewithtax.Replace(".", ","));
                civ.IsProductAvailabeToOrder = true;
                civ.ProductStockTrackingIsEnabled = false;
                //Core.Models.Media pti = new ProductThumbnail().;
                civ.ProductImage = ln.imagesmall;
                civ.Quantity = decimal.ToInt32(decimal.Parse(ln.quantity.Replace(".", ",")));


                //        Id = x.Id,
                //        ProductId = x.ProductId,
                //        ProductName = x.Product.Name,
                //        ProductPrice = x.Product.Price,
                //        ProductStockQuantity = x.Product.StockQuantity,
                //        ProductStockTrackingIsEnabled = x.Product.StockTrackingIsEnabled,
                //        IsProductAvailabeToOrder = x.Product.IsAllowToOrder && x.Product.IsPublished && !x.Product.IsDeleted,
                //        ProductImage = _mediaService.GetThumbnailUrl(x.Product.ThumbnailImage),
                //        Quantity = x.Quantity,

                cartVm.Items.Add(civ);
            }
            cartVm.SubTotal = decimal.Parse(blc.totalwithtax.Replace(".", ","));
            cartVm.Discount = 0;
            return cartVm;
            //Datos esperados, los mismos que devuelve await _cartService.GetActiveCartDetails(currentUser.Id);
            //var cartVm = new CartVm(_currencyService)
            //{
            //    Id = cart.Id,
            //    CouponCode = cart.CouponCode,
            //    IsProductPriceIncludeTax = cart.IsProductPriceIncludeTax,
            //    TaxAmount = cart.TaxAmount,
            //    ShippingAmount = cart.ShippingAmount,
            //    OrderNote = cart.OrderNote
            //};

            //cartVm.Items = _cartItemRepository
            //    .Query()
            //    .Include(x => x.Product).ThenInclude(p => p.ThumbnailImage)
            //    .Include(x => x.Product).ThenInclude(p => p.OptionCombinations).ThenInclude(o => o.Option)
            //    .Where(x => x.CartId == cart.Id).ToList()
            //    .Select(x => new CartItemVm(_currencyService)
            //    {
            //        Id = x.Id,
            //        ProductId = x.ProductId,
            //        ProductName = x.Product.Name,
            //        ProductPrice = x.Product.Price,
            //        ProductStockQuantity = x.Product.StockQuantity,
            //        ProductStockTrackingIsEnabled = x.Product.StockTrackingIsEnabled,
            //        IsProductAvailabeToOrder = x.Product.IsAllowToOrder && x.Product.IsPublished && !x.Product.IsDeleted,
            //        ProductImage = _mediaService.GetThumbnailUrl(x.Product.ThumbnailImage),
            //        Quantity = x.Quantity,
            //        VariationOptions = CartItemVm.GetVariationOption(x.Product)
            //    }).ToList();

            //cartVm.SubTotal = cartVm.Items.Sum(x => x.Quantity * x.ProductPrice);
            //if (!string.IsNullOrWhiteSpace(cartVm.CouponCode))
            //{
            //    var cartInfoForCoupon = new CartInfoForCoupon
            //    {
            //        Items = cartVm.Items.Select(x => new CartItemForCoupon { ProductId = x.ProductId, Quantity = x.Quantity }).ToList()
            //    };
            //    var couponValidationResult = await _couponService.Validate(customerId, cartVm.CouponCode, cartInfoForCoupon);
            //    if (couponValidationResult.Succeeded)
            //    {
            //        cartVm.Discount = couponValidationResult.DiscountAmount;
            //    }
            //    else
            //    {
            //        cartVm.CouponValidationErrorMessage = couponValidationResult.ErrorMessage;
            //    }
            //}
        }


    }
}
