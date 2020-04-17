using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CRM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SimplCommerce.Infrastructure.Web;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.Core.Services;
using SimplCommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;
using SimplCommerce.Module.ShoppingCart.Models;
using SimplCommerce.Module.ShoppingCart.Services;

namespace SimplCommerce.Module.Orders.Areas.Orders.Components
{
    public class OrderSummaryViewComponent : ViewComponent
    {
        private readonly ICartService _cartService;
        private readonly IWorkContext _workContext;
        private ICurrencyService _currencyService;

        public OrderSummaryViewComponent(ICartService cartService, IWorkContext workContext, ICurrencyService currencyService)
        {
            _cartService = cartService;
            _workContext = workContext;
            _currencyService = currencyService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var curentUser = await _workContext.GetCurrentUser();
            //aqui cargamos datacarrito
            var cart2 = await _cartService.GetActiveCartDetails(curentUser.Id);
            var cart = DataCarrito();
            if (cart != null && cart2 != null)
            {
                if (cart.Items.Count == cart2.Items.Count)
                {
                    decimal totalcarrito = decimal.Zero;
                    decimal totalcart = decimal.Zero;
                    foreach (CartItemVm ci in cart.Items)
                    {
                        totalcarrito += (ci.ProductPrice * ci.Quantity);
                    }
                    foreach (CartItemVm ci in cart2.Items)
                    {
                        var p = RecuperaArtículo(GetIP(), GetSession(), ci.ProductId);
                        totalcart += (decimal.Parse(p.result.pricewithtax) * ci.Quantity);
                    }
                    if (totalcart != totalcarrito)
                    {
                        cart.SubTotal = totalcarrito;
                    }
                    cart.ShippingAmount = cart2.ShippingAmount;
                }
            }
            if (cart2 == null)
            {
                cart2 = new CartVm(_currencyService);
            }
            return View(this.GetViewPath(), cart);
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
                string json = "{\"token\":\"" + _sesionToken + "\",\"ipbase64\":\"" + laip + "\",\"line\":\"" + linea + "\"}";
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

        public CartVm DataCarrito()
        {
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
                int sta = 0;
                _ = int.TryParse(arti.result.stocks, out sta);
                civ.ProductStockQuantity = decimal.ToInt32(decimal.Parse(arti.result.stocks));
                civ.ProductName = ln.description;
                civ.ProductPrice = decimal.Parse(ln.pricewithtax);
                civ.IsProductAvailabeToOrder = true;
                civ.ProductStockTrackingIsEnabled = false;
                //Core.Models.Media pti = new ProductThumbnail().;
                civ.ProductImage = ln.imagesmall;
                civ.Quantity = decimal.ToInt32(decimal.Parse(ln.quantity));
                cartVm.Items.Add(civ);
            }
            cartVm.SubTotal = decimal.Parse(blc.totalwithtax);
            cartVm.Discount = 0;
            return cartVm;
        }

    }
}
