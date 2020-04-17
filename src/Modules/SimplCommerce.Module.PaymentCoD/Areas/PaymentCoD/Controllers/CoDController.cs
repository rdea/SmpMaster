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
using Newtonsoft.Json;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Core.Extensions;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Orders.Services;
using SimplCommerce.Module.PaymentCoD.Models;
using SimplCommerce.Module.Payments.Models;
using SimplCommerce.Module.ShoppingCart.Areas.ShoppingCart.ViewModels;
using SimplCommerce.Module.ShoppingCart.Services;

namespace SimplCommerce.Module.PaymentCoD.Areas.PaymentCoD.Controllers
{
    [Authorize]
    [Area("PaymentCoD")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class CoDController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly ICartService _cartService;
        private readonly IRepositoryWithTypedId<PaymentProvider, string> _paymentProviderRepository;
        private Lazy<CoDSetting> _setting;

        public CoDController(
            ICartService cartService,
            IOrderService orderService,
            IRepositoryWithTypedId<PaymentProvider, string> paymentProviderRepository,
            IWorkContext workContext)
        {
            _paymentProviderRepository = paymentProviderRepository;
            _cartService = cartService;
            _orderService = orderService;
            _workContext = workContext;
            _setting = new Lazy<CoDSetting>(GetSetting());
        }

        public async Task<IActionResult> CoDCheckout()
        {
            var currentUser = await _workContext.GetCurrentUser();
            var cart = await _cartService.GetActiveCartDetails(currentUser.Id);
            decimal totalcarrito = decimal.Zero;

            foreach (CartItemVm ci in cart.Items)
            {
                var p = RecuperaArtículo(GetIP(), GetSession(), ci.ProductId);
                ci.ProductPrice = decimal.Parse(p.result.pricewithtax);
                ci.ProductName = p.result.description;
                totalcarrito += (ci.ProductPrice * ci.Quantity);
            }
            cart.SubTotal = totalcarrito;
            if (cart == null)
            {
                return NotFound();
            }

            if (!ValidateCoD(cart))
            {
                TempData["Error"] = "Payment Method is not eligible for this order.";
                return Redirect("~/checkout/payment");
            }

            var calculatedFee = CalculateFee(cart);      
             // revisar este procedimiento, da error
            var orderCreateResult = await _orderService.CreateOrder(cart.Id, "CashOnDelivery", calculatedFee,OrderStatus.New,GetIP(), GetSession());

            if (!orderCreateResult.Success)
            {
                TempData["Error"] = orderCreateResult.Error;
                return Redirect("~/checkout/payment");
            }

            return Redirect($"~/checkout/success?orderId={orderCreateResult.Value.Id}");
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


        private CoDSetting GetSetting()
        {
            var coDProvider = _paymentProviderRepository.Query().FirstOrDefault(x => x.Id == PaymentProviderHelper.CODProviderId);
            if (string.IsNullOrEmpty(coDProvider.AdditionalSettings))
            {
                return new CoDSetting();
            }

            var coDSetting = JsonConvert.DeserializeObject<CoDSetting>(coDProvider.AdditionalSettings);
            return coDSetting;
        }

        private bool ValidateCoD(CartVm cart)
        {
            if (_setting.Value.MinOrderValue.HasValue && _setting.Value.MinOrderValue.Value > cart.OrderTotal)
            {
                return false;
            }

            if (_setting.Value.MaxOrderValue.HasValue && _setting.Value.MaxOrderValue.Value < cart.OrderTotal)
            {
                return false;
            }

            return true;
        }

        private decimal CalculateFee(CartVm cart)
        {
            var percent = _setting.Value.PaymentFee;
            return (cart.OrderTotal / 100) * percent;
        }
    }
}
