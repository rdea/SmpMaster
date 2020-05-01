using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Orders.Areas.Orders.ViewModels;
using SimplCommerce.Module.Orders.Services;
using SimplCommerce.Module.ShippingPrices.Services;
using SimplCommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;
using SimplCommerce.Module.ShoppingCart.Models;
using SimplCommerce.Module.ShoppingCart.Services;

namespace SimplCommerce.Module.Orders.Areas.Orders.Controllers
{
    [Area("Orders")]
    [Route("checkout")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CheckoutController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IRepositoryWithTypedId<Country, string> _countryRepository;
        private readonly IRepository<StateOrProvince> _stateOrProvinceRepository;
        private readonly IRepository<UserAddress> _userAddressRepository;
        private readonly IShippingPriceService _shippingPriceService;
        private readonly ICartService _cartService;
        private readonly IWorkContext _workContext;
        private readonly IRepository<Cart> _cartRepository;

        public CheckoutController(
            IRepository<StateOrProvince> stateOrProvinceRepository,
            IRepositoryWithTypedId<Country, string> countryRepository,
            IRepository<UserAddress> userAddressRepository,
            IShippingPriceService shippingPriceService,
            IOrderService orderService,
            ICartService cartService,
            IWorkContext workContext,
            IRepository<Cart> cartRepository)
        {
            _stateOrProvinceRepository = stateOrProvinceRepository;
            _countryRepository = countryRepository;
            _userAddressRepository = userAddressRepository;
            _shippingPriceService = shippingPriceService;
            _orderService = orderService;
            _cartService = cartService;
            _workContext = workContext;
            _cartRepository = cartRepository;
        }

        [HttpGet("shipping")]
        public async Task<IActionResult> Shipping()
        {
            //despues de login vamos aqui
            // codigo origen
            //var currentUser = await _workContext.GetCurrentUser();
            //debug este procediiento, devuelve carrito vacio.
            //var cart = await _cartService.GetActiveCartDetails(currentUser.Id);
            //if (cart == null || !cart.Items.Any())
            //{
            //    return Redirect("~/");
            //}

            //var model = new DeliveryInformationVm();

            //PopulateShippingForm(model, currentUser);
            
            var currentUser = await _workContext.GetCurrentUser();
            var cart = DataCart();

            //cargamos los datos de nuestro carrito
            if (cart == null || !cart.Items.Any())
            {
                return Redirect("~/");
            }

            var model = new DeliveryInformationVm();

            PopulateShippingForm(model, currentUser);

            return View(model);
        }
        public Cart DataCart()
        {
            //GetStructCarrito(GetIP(), GetSession());
            //var blc = RecuperaTotalCarrito(GetIP(), GetSession());
            var todas = RecuperatodasLineasCarrito(GetIP(), GetSession());
            Random r = new Random();

            // int totalLineas = int.Parse(blc.totallines);
            var cartVm = new Cart()
            {
                
                //Id = r.Next(0, 60000),//int.Parse(DateTime.Now.ToString()),
                IsProductPriceIncludeTax = true,
                //TaxAmount = cart.TaxAmount,
                //ShippingAmount = cart.ShippingAmount,
                //OrderNote = cart.OrderNote
            };

            foreach (CartLine ln in todas.result)
            {
                CartItem civ = new CartItem();
                
                civ.ProductId = long.Parse(ln.identifier);
                var arti = RecuperaArtículo(GetIP(), GetSession(), civ.ProductId);
                int sta = 0;
                _ = int.TryParse(arti.result.stocks, out sta);
                civ.Product = new Catalog.Models.Product();
                civ.Product.Name = ln.description;
                civ.Product.Price = decimal.Parse(ln.pricewithtax.Replace(".", ","));
                civ.Product.Name= ln.description; 
                civ.Quantity = decimal.ToInt32(decimal.Parse(ln.quantity));
                cartVm.Items.Add(civ);
            }
            
            return cartVm;
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
        public DataCollection<CartLine> RecuperatodasLineasCarrito(string laip, string _sesionToken)
        {
            var result = new DataCollection<CartLine>();

            //string de url principal
            string urlPath = "https://riews.reinfoempresa.com:8443";
            // string filtro = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("'@[{"field":"REFERENCIA","operator":" >= ","fieldliteral":"0","type":"text","connector":""}]'"));
            string statement = @"[{""statementv1"":[{""field"":""LINE"",""operator"":"">="",""fieldliteral"":""0"",""type"":""text"",""connector"":""""}]}]";
            // string statement = @"[]";
            statement = System.Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(statement));
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

        [HttpPost("shipping")]
        public async Task<IActionResult> Shipping(DeliveryInformationVm model)
        {
            var currentUser = await _workContext.GetCurrentUser();
            // TODO Handle error messages
            if ((!model.NewAddressForm.IsValid() && model.ShippingAddressId == 0) ||
                (!model.NewBillingAddressForm.IsValid() && !model.UseShippingAddressAsBillingAddress && model.BillingAddressId == 0))
            {
                PopulateShippingForm(model, currentUser);
                return View(model);
            }

            var cart = await _cartService.GetActiveCart(currentUser.Id);

            if (cart == null)
            {
                throw new ApplicationException($"Cart of user {currentUser.Id} cannot be found");
            }

            cart.ShippingData = JsonConvert.SerializeObject(model);
            await _cartRepository.SaveChangesAsync();
            return Redirect("~/checkout/payment");
        }

        [HttpPost("update-tax-and-shipping-prices")]
        public async Task<IActionResult> UpdateTaxAndShippingPrices([FromBody] TaxAndShippingPriceRequestVm model)
        {
            //codigo origen
            //  var currentUser = await _workContext.GetCurrentUser();
            //var cart = await _cartService.GetActiveCart(currentUser.Id);
            //var orderTaxAndShippingPrice = await _orderService.UpdateTaxAndShippingPrices(cart.Id, model);

            //obtenemos usuario
            var currentUser = await _workContext.GetCurrentUser();
            //comprobamos si tiene carritos activos, si los tiene, los desactivamos para grabar uno nuevo.
            bool obtenerCarrito = false;
            var cart = await _cartService.GetActiveCart(currentUser.Id);
            var carrito = DataCart();
            if (cart == null && carrito != null)
            {
                foreach (CartItem ci in carrito.Items)
                {
                    await _cartService.AddToCart(currentUser.Id, ci.ProductId, ci.Quantity);
                }

            }
            if (cart!= null && carrito != null)
            {
                if (cart.Items.Count == carrito.Items.Count)
                {
                    decimal totalcarrito = decimal.Zero;
                    decimal totalcart = decimal.Zero;
                    foreach (CartItem ci in carrito.Items)
                    {
                        totalcarrito += (ci.Product.Price * ci.Quantity);
                    }
                    foreach(CartItem ci in cart.Items)
                    {
                        var p = RecuperaArtículo(GetIP(), GetSession(), ci.ProductId);
                        totalcart += (decimal.Parse(p.result.pricewithtax.Replace(".", ",")) * ci.Quantity);
                    }
                    if(totalcart != totalcarrito)
                    {
                        cart.IsActive = false;
                        _cartRepository.SaveChanges();
                        //añadimos lineas a un nuevo carrito
                        foreach (CartItem ci in carrito.Items)
                        {
                            await _cartService.AddToCart(currentUser.Id, ci.ProductId, ci.Quantity);
                        }
                        obtenerCarrito = true;
                        //volvemos a obtener el carrito
                    }
                }
                else
                {
                    cart.IsActive = false;
                    _cartRepository.SaveChanges();
                    //añadimos lineas a un nuevo carrito
                    foreach (CartItem ci in carrito.Items)
                    {
                        await _cartService.AddToCart(currentUser.Id, ci.ProductId, ci.Quantity);
                    }
                    obtenerCarrito = true;
                    //volvemos a obtener el carrito
                }

            }
            
            //obtenemos carrito para confirmar
            if (obtenerCarrito)
            {
                var cart2 = await _cartService.GetActiveCart(currentUser.Id);

                //devuelve precios de envios
                var orderTaxAndShippingPrice = await _orderService.UpdateTaxAndShippingPrices(cart2.Id, model);
                //despues de esto, tenemos que revisar el precio del order, esto nos devuelve los gastos de envio, pero no el total del pedido.
                foreach (CartItem ci in carrito.Items)
                {
                    orderTaxAndShippingPrice.Cart.SubTotal += (ci.Product.Price * ci.Quantity);
                }
                return Ok(orderTaxAndShippingPrice);
            }
            else
            {
                //devuelve precios de envios
                var orderTaxAndShippingPrice = await _orderService.UpdateTaxAndShippingPrices(cart.Id, model);
                //despues de esto, tenemos que revisar el precio del order, esto nos devuelve los gastos de envio, pero no el total del pedido.
                foreach (CartItem ci in carrito.Items)
                {
                    orderTaxAndShippingPrice.Cart.SubTotal += (ci.Product.Price * ci.Quantity);
                }
                return Ok(orderTaxAndShippingPrice);
            }
            //cart2.ShippingAmount = orderTaxAndShippingPrice.ShippingPrices[0].Price;
            //await _cartRepository.SaveChangesAsync();
        }

        [HttpGet("success")]
        public IActionResult Success(long orderId)
        {
            return View(orderId);
        }

        [HttpGet("error")]
        public IActionResult Error(long orderId)
        {
            return View(orderId);
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> Cancel()
        {
            var currentUser = await _workContext.GetCurrentUser();
            var cart = await _cartService.GetActiveCart(currentUser.Id);
            if(cart != null && cart.LockedOnCheckout)
            {
                cart.LockedOnCheckout = false;
                await _cartRepository.SaveChangesAsync();
            }

            return Redirect("~/");
        }

        private void PopulateShippingForm(DeliveryInformationVm model, User currentUser)
        {
            model.ExistingShippingAddresses = _userAddressRepository
                .Query()
                .Where(x => (x.AddressType == AddressType.Shipping) && (x.UserId == currentUser.Id))
                .Select(x => new ShippingAddressVm
                {
                    UserAddressId = x.Id,
                    ContactName = x.Address.ContactName,
                    Phone = x.Address.Phone,
                    AddressLine1 = x.Address.AddressLine1,
                    CityName = x.Address.City,
                    ZipCode = x.Address.ZipCode,
                    DistrictName = x.Address.District.Name,
                    StateOrProvinceName = x.Address.StateOrProvince.Name,
                    CountryName = x.Address.Country.Name,
                    IsCityEnabled = x.Address.Country.IsCityEnabled,
                    IsZipCodeEnabled = x.Address.Country.IsZipCodeEnabled,
                    IsDistrictEnabled = x.Address.Country.IsDistrictEnabled
                }).ToList();

            model.ShippingAddressId = currentUser.DefaultShippingAddressId ?? 0;

            model.UseShippingAddressAsBillingAddress = true;

            model.NewAddressForm.ShipableContries = _countryRepository.Query()
                .Where(x => x.IsShippingEnabled)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                }).ToList();

            if (model.NewAddressForm.ShipableContries.Count == 1)
            {
                var onlyShipableCountryId = model.NewAddressForm.ShipableContries.First().Value;

                model.NewAddressForm.StateOrProvinces = _stateOrProvinceRepository
                .Query()
                .Where(x => x.CountryId == onlyShipableCountryId)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                }).ToList();
            }
        }
    }
}
