﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Orders.Areas.Orders.ViewModels;
using SimplCommerce.Module.Orders.Models;
using SimplCommerce.Module.Pricing.Services;
using SimplCommerce.Module.ShippingPrices.Services;
using SimplCommerce.Module.ShoppingCart.Models;
using SimplCommerce.Module.Tax.Services;
using SimplCommerce.Module.Orders.Events;
using SimplCommerce.Module.ShoppingCart.Services;
using System.Net;
using System.IO;
using CRM.Models;

namespace SimplCommerce.Module.Orders.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly ICouponService _couponService;
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly ITaxService _taxService;
        private readonly ICartService _cartService;
        private readonly IShippingPriceService _shippingPriceService;
        private readonly IRepository<UserAddress> _userAddressRepository;
        private readonly IMediator _mediator;

        public OrderService(IRepository<Order> orderRepository,
            IRepository<Cart> cartRepository,
            ICouponService couponService,

            IRepository<CartItem> cartItemRepository,
            IRepository<OrderItem> orderItemRepository,
            ITaxService taxService,
            ICartService cartService,
            IShippingPriceService shippingPriceService,
            IRepository<UserAddress> userAddressRepository,
            IMediator mediator)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _couponService = couponService;
            _cartItemRepository = cartItemRepository;
            _orderItemRepository = orderItemRepository;
            _taxService = taxService;
            _cartService = cartService;
            _shippingPriceService = shippingPriceService;
            _userAddressRepository = userAddressRepository;
            _mediator = mediator;
        }
        public async Task<Result<Order>> CreateOrder(long cartId, string paymentMethod, decimal paymentFeeAmount, OrderStatus orderStatus = OrderStatus.New)
        {
            var cart = await _cartRepository
               .Query()
               .Where(x => x.Id == cartId).FirstOrDefaultAsync();

            if (cart == null)
            {
                return Result.Fail<Order>($"Cart id {cartId} cannot be found");
            }

            var shippingData = JsonConvert.DeserializeObject<DeliveryInformationVm>(cart.ShippingData);
            Address billingAddress;
            Address shippingAddress;
            if (shippingData.ShippingAddressId == 0)
            {
                var address = new Address
                {
                    AddressLine1 = shippingData.NewAddressForm.AddressLine1,
                    AddressLine2 = shippingData.NewAddressForm.AddressLine2,
                    ContactName = shippingData.NewAddressForm.ContactName,
                    CountryId = shippingData.NewAddressForm.CountryId,
                    StateOrProvinceId = shippingData.NewAddressForm.StateOrProvinceId,
                    DistrictId = shippingData.NewAddressForm.DistrictId,
                    City = shippingData.NewAddressForm.City,
                    ZipCode = shippingData.NewAddressForm.ZipCode,
                    Phone = shippingData.NewAddressForm.Phone
                };

                var userAddress = new UserAddress
                {
                    Address = address,
                    AddressType = AddressType.Shipping,
                    UserId = cart.CustomerId
                };

                _userAddressRepository.Add(userAddress);

                shippingAddress = address;
            }
            else
            {
                shippingAddress = _userAddressRepository.Query().Where(x => x.Id == shippingData.ShippingAddressId).Select(x => x.Address).First();
            }

            if (shippingData.UseShippingAddressAsBillingAddress)
            {
                billingAddress = shippingAddress;
            }
            else if (shippingData.BillingAddressId == 0)
            {
                var address = new Address
                {
                    AddressLine1 = shippingData.NewBillingAddressForm.AddressLine1,
                    AddressLine2 = shippingData.NewBillingAddressForm.AddressLine2,
                    ContactName = shippingData.NewBillingAddressForm.ContactName,
                    CountryId = shippingData.NewBillingAddressForm.CountryId,
                    StateOrProvinceId = shippingData.NewBillingAddressForm.StateOrProvinceId,
                    DistrictId = shippingData.NewBillingAddressForm.DistrictId,
                    City = shippingData.NewBillingAddressForm.City,
                    ZipCode = shippingData.NewBillingAddressForm.ZipCode,
                    Phone = shippingData.NewBillingAddressForm.Phone
                };

                var userAddress = new UserAddress
                {
                    Address = address,
                    AddressType = AddressType.Billing,
                    UserId = cart.CustomerId
                };

                _userAddressRepository.Add(userAddress);

                billingAddress = address;
            }
            else
            {
                billingAddress = _userAddressRepository.Query().Where(x => x.Id == shippingData.BillingAddressId).Select(x => x.Address).First();
            }

            return await CreateOrder(cartId, paymentMethod, paymentFeeAmount, shippingData.ShippingMethod, billingAddress, shippingAddress, orderStatus);
        }

        public async Task<Result<Order>> CreateOrder(long cartId, string paymentMethod, decimal paymentFeeAmount, OrderStatus orderStatus = OrderStatus.New, string ip = "1" , string token= "1")
        {
            var cart = await _cartRepository
               .Query()
               .Where(x => x.Id == cartId).FirstOrDefaultAsync();

            if (cart == null)
            {
                return Result.Fail<Order>($"Cart id {cartId} cannot be found");
            }

            var shippingData = JsonConvert.DeserializeObject<DeliveryInformationVm>(cart.ShippingData);
            Address billingAddress;
            Address shippingAddress;
            if (shippingData.ShippingAddressId == 0)
            {
                var address = new Address
                {
                    AddressLine1 = shippingData.NewAddressForm.AddressLine1,
                    AddressLine2 = shippingData.NewAddressForm.AddressLine2,
                    ContactName = shippingData.NewAddressForm.ContactName,
                    CountryId = shippingData.NewAddressForm.CountryId,
                    StateOrProvinceId = shippingData.NewAddressForm.StateOrProvinceId,
                    DistrictId = shippingData.NewAddressForm.DistrictId,
                    City = shippingData.NewAddressForm.City,
                    ZipCode = shippingData.NewAddressForm.ZipCode,
                    Phone = shippingData.NewAddressForm.Phone
                };

                var userAddress = new UserAddress
                {
                    Address = address,
                    AddressType = AddressType.Shipping,
                    UserId = cart.CustomerId
                };

                _userAddressRepository.Add(userAddress);

                shippingAddress = address;
            }
            else
            {
                shippingAddress = _userAddressRepository.Query().Where(x => x.Id == shippingData.ShippingAddressId).Select(x => x.Address).First();
            }

            if (shippingData.UseShippingAddressAsBillingAddress)
            {
                billingAddress = shippingAddress;
            }
            else if (shippingData.BillingAddressId == 0)
            {
                var address = new Address
                {
                    AddressLine1 = shippingData.NewBillingAddressForm.AddressLine1,
                    AddressLine2 = shippingData.NewBillingAddressForm.AddressLine2,
                    ContactName = shippingData.NewBillingAddressForm.ContactName,
                    CountryId = shippingData.NewBillingAddressForm.CountryId,
                    StateOrProvinceId = shippingData.NewBillingAddressForm.StateOrProvinceId,
                    DistrictId = shippingData.NewBillingAddressForm.DistrictId,
                    City = shippingData.NewBillingAddressForm.City,
                    ZipCode = shippingData.NewBillingAddressForm.ZipCode,
                    Phone = shippingData.NewBillingAddressForm.Phone
                };

                var userAddress = new UserAddress
                {
                    Address = address,
                    AddressType = AddressType.Billing,
                    UserId = cart.CustomerId
                };

                _userAddressRepository.Add(userAddress);

                billingAddress = address;
            }
            else
            {
                billingAddress = _userAddressRepository.Query().Where(x => x.Id == shippingData.BillingAddressId).Select(x => x.Address).First();
            }
            if (ip != "1" && token != "1" && cart!=null)
            {
                decimal totalcarrito = decimal.Zero;

                foreach (CartItem ci in cart.Items)
                {
                    var p = RecuperaArtículo(ip, token, ci.ProductId);
                    if (ci.Product == null)
                    {
                        ci.Product = new Catalog.Models.Product();
                    }
                    ci.Product.Price = decimal.Parse(p.result.pricewithtax.Replace(".", ","));
                    ci.Product.Name = p.result.description;
                    ci.Product.Slug = ci.Product.Name.Replace(" ", "-") + "-" + ci.ProductId;
                    totalcarrito += (ci.Product.Price * ci.Quantity);
                }
            }
           Result<Order> resultado = await CreateOrder(cartId, paymentMethod, paymentFeeAmount, shippingData.ShippingMethod, billingAddress, shippingAddress, orderStatus);
            //si resultado ok, subimos compra al WS
            
            if (resultado.Success)
            {
                var orderc = SubeCompra(ip, token, cart, shippingAddress, billingAddress);
                if (orderc.status == "OK")
                {
                    var r = "hola";
                }
                else if (orderc.status == "855")
                { 

                }
            }
            return resultado;
        }

        public async Task<Result<Order>> CreateOrder(long cartId, string paymentMethod, decimal paymentFeeAmount, string shippingMethodName, Address billingAddress, Address shippingAddress, OrderStatus orderStatus = OrderStatus.New)
        {
            var cart = _cartRepository
                .Query()
                .Include(c => c.Items).ThenInclude(x => x.Product)
                .Where(x => x.Id == cartId).FirstOrDefault();

            if (cart == null)
            {
                return Result.Fail<Order>($"Cart id {cartId} cannot be found");
            }

            var checkingDiscountResult = await CheckForDiscountIfAny(cart);
            if (!checkingDiscountResult.Succeeded)
            {
                return Result.Fail<Order>(checkingDiscountResult.ErrorMessage);
            }

            var validateShippingMethodResult = await ValidateShippingMethod(shippingMethodName, shippingAddress, cart);
            if (!validateShippingMethodResult.Success)
            {
                return Result.Fail<Order>(validateShippingMethodResult.Error);
            }

            var shippingMethod = validateShippingMethodResult.Value;

            var orderBillingAddress = new OrderAddress()
            {
                AddressLine1 = billingAddress.AddressLine1,
                AddressLine2 = billingAddress.AddressLine2,
                ContactName = billingAddress.ContactName,
                CountryId = billingAddress.CountryId,
                StateOrProvinceId = billingAddress.StateOrProvinceId,
                DistrictId = billingAddress.DistrictId,
                City = billingAddress.City,
                ZipCode = billingAddress.ZipCode,
                Phone = billingAddress.Phone
            };

            var orderShippingAddress = new OrderAddress()
            {
                AddressLine1 = shippingAddress.AddressLine1,
                AddressLine2 = shippingAddress.AddressLine2,
                ContactName = shippingAddress.ContactName,
                CountryId = shippingAddress.CountryId,
                StateOrProvinceId = shippingAddress.StateOrProvinceId,
                DistrictId = shippingAddress.DistrictId,
                City = shippingAddress.City,
                ZipCode = shippingAddress.ZipCode,
                Phone = shippingAddress.Phone
            };

            var order = new Order
            {
                CustomerId = cart.CustomerId,
                CreatedOn = DateTimeOffset.Now,
                CreatedById = cart.CreatedById,
                LatestUpdatedOn = DateTimeOffset.Now,
                LatestUpdatedById = cart.CreatedById,
                BillingAddress = orderBillingAddress,
                ShippingAddress = orderShippingAddress,
                PaymentMethod = paymentMethod,
                PaymentFeeAmount = paymentFeeAmount
            };

            foreach (var cartItem in cart.Items)
            {
                //codigo origen, necesairio revisar validaciones para la compra tipo stock etc
                //if (!cartItem.Product.IsAllowToOrder || !cartItem.Product.IsPublished || cartItem.Product.IsDeleted)
                //{
                //    return Result.Fail<Order>($"The product {cartItem.Product.Name} is not available any more");
                //}

                //if (cartItem.Product.StockTrackingIsEnabled && cartItem.Product.StockQuantity < cartItem.Quantity)
                //{
                //    return Result.Fail<Order>($"There are only {cartItem.Product.StockQuantity} items available for {cartItem.Product.Name}");
                //}

                var taxPercent = await _taxService.GetTaxPercent(cartItem.Product.TaxClassId, shippingAddress.CountryId, shippingAddress.StateOrProvinceId, shippingAddress.ZipCode);
                var productPrice = cartItem.Product.Price;
                if (cart.IsProductPriceIncludeTax)
                {
                    productPrice = productPrice / (1 + (taxPercent / 100));
                }

                var orderItem = new OrderItem
                {
                    Product = cartItem.Product,
                    ProductPrice = productPrice,
                    Quantity = cartItem.Quantity,
                    TaxPercent = taxPercent,
                    TaxAmount = cartItem.Quantity * (productPrice * taxPercent / 100)
                };

                var discountedItem = checkingDiscountResult.DiscountedProducts.FirstOrDefault(x => x.Id == cartItem.ProductId);
                if (discountedItem != null)
                {
                    orderItem.DiscountAmount = discountedItem.DiscountAmount;
                }

                order.AddOrderItem(orderItem);
                if (cartItem.Product.StockTrackingIsEnabled)
                {
                    cartItem.Product.StockQuantity = cartItem.Product.StockQuantity - cartItem.Quantity;
                }
            }

            order.OrderStatus = orderStatus;
            order.OrderNote = cart.OrderNote;
            order.CouponCode = checkingDiscountResult.CouponCode;
            order.CouponRuleName = cart.CouponRuleName;
            order.DiscountAmount = checkingDiscountResult.DiscountAmount;
            order.ShippingFeeAmount = shippingMethod.Price;
            order.ShippingMethod = shippingMethod.Name;
            order.TaxAmount = order.OrderItems.Sum(x => x.TaxAmount);
            order.SubTotal = order.OrderItems.Sum(x => x.ProductPrice * x.Quantity);
            order.SubTotalWithDiscount = order.SubTotal - checkingDiscountResult.DiscountAmount;
            order.OrderTotal = order.SubTotal + order.TaxAmount + order.ShippingFeeAmount + order.PaymentFeeAmount - order.DiscountAmount;
            _orderRepository.Add(order);

            cart.IsActive = false;

            var vendorIds = cart.Items.Where(x => x.Product.VendorId.HasValue).Select(x => x.Product.VendorId.Value).Distinct();
            if (vendorIds.Any())
            {
                order.IsMasterOrder = true;
            }

            IList<Order> subOrders = new List<Order>();
            foreach (var vendorId in vendorIds)
            {
                var subOrder = new Order
                {
                    CustomerId = cart.CustomerId,
                    CreatedOn = DateTimeOffset.Now,
                    CreatedById = cart.CreatedById,
                    LatestUpdatedOn = DateTimeOffset.Now,
                    LatestUpdatedById = cart.CreatedById,
                    BillingAddress = orderBillingAddress,
                    ShippingAddress = orderShippingAddress,
                    VendorId = vendorId,
                    Parent = order
                };

                foreach (var cartItem in cart.Items.Where(x => x.Product.VendorId == vendorId))
                {
                    var taxPercent = await _taxService.GetTaxPercent(cartItem.Product.TaxClassId, shippingAddress.CountryId, shippingAddress.StateOrProvinceId, shippingAddress.ZipCode);
                    var productPrice = cartItem.Product.Price;
                    if (cart.IsProductPriceIncludeTax)
                    {
                        productPrice = productPrice / (1 + (taxPercent / 100));
                    }

                    var orderItem = new OrderItem
                    {
                        Product = cartItem.Product,
                        ProductPrice = productPrice,
                        Quantity = cartItem.Quantity,
                        TaxPercent = taxPercent,
                        TaxAmount = cartItem.Quantity * (productPrice * taxPercent / 100)
                    };

                    if (cart.IsProductPriceIncludeTax)
                    {
                        orderItem.ProductPrice = orderItem.ProductPrice - orderItem.TaxAmount;
                    }

                    subOrder.AddOrderItem(orderItem);
                }

                subOrder.SubTotal = subOrder.OrderItems.Sum(x => x.ProductPrice * x.Quantity);
                subOrder.TaxAmount = subOrder.OrderItems.Sum(x => x.TaxAmount);
                subOrder.OrderTotal = subOrder.SubTotal + subOrder.TaxAmount + subOrder.ShippingFeeAmount - subOrder.DiscountAmount;
                _orderRepository.Add(subOrder);
                subOrders.Add(subOrder);
            }

            using (var transaction = _orderRepository.BeginTransaction())
            {
                _orderRepository.SaveChanges();
                await PublishOrderCreatedEvent(order);
                foreach (var subOrder in subOrders)
                {
                    await PublishOrderCreatedEvent(subOrder);
                }

                _couponService.AddCouponUsage(cart.CustomerId, order.Id, checkingDiscountResult);
                _orderRepository.SaveChanges();
                transaction.Commit();
            }
            //aqui tenemos que borrar el carrito
            
            return Result.Ok(order);
        }

        private async Task PublishOrderCreatedEvent(Order order)
        {
            var orderCreated = new OrderCreated
            {
                OrderId = order.Id,
                Order = order,
                UserId = order.CreatedById,
                Note = order.OrderNote
            };

            await _mediator.Publish(orderCreated);
        }

        public void CancelOrder(Order order)
        {
            order.OrderStatus = OrderStatus.Canceled;
            order.LatestUpdatedOn = DateTimeOffset.Now;

            var orderItems = _orderItemRepository.Query().Include(x => x.Product).Where(x => x.Order.Id == order.Id);
            foreach (var item in orderItems)
            {
                if (item.Product.StockTrackingIsEnabled)
                {
                    item.Product.StockQuantity = item.Product.StockQuantity + item.Quantity;
                }
            }
        }

        public async Task<decimal> GetTax(long cartId, string countryId, long stateOrProvinceId, string zipCode)
        {
            decimal taxAmount = 0;

            var cartItems = _cartItemRepository.Query()
                .Where(x => x.CartId == cartId)
                .Select(x => new CartItemVm
                {
                    Quantity = x.Quantity,
                    Price = x.Product.Price,
                    TaxClassId = x.Product.TaxClass.Id
                }).ToList();

            foreach (var cartItem in cartItems)
            {
                if (cartItem.TaxClassId.HasValue)
                {
                    var taxRate = await _taxService.GetTaxPercent(cartItem.TaxClassId, countryId, stateOrProvinceId, zipCode);
                    taxAmount = taxAmount + cartItem.Quantity * cartItem.Price * taxRate / 100;
                }
            }

            return taxAmount;
        }

        public async Task<OrderTaxAndShippingPriceVm> UpdateTaxAndShippingPrices(long cartId, TaxAndShippingPriceRequestVm model)
        {
            var cart = await _cartRepository.Query().FirstOrDefaultAsync(x => x.Id == cartId);
            if (cart == null)
            {
                throw new ApplicationException($"Cart id {cartId} not found");
            }

            Address address;
            if (model.ExistingShippingAddressId != 0)
            {
                address = await _userAddressRepository.Query().Where(x => x.Id == model.ExistingShippingAddressId).Select(x => x.Address).FirstOrDefaultAsync();
                if (address == null)
                {
                    throw new ApplicationException($"Address id {model.ExistingShippingAddressId} not found");
                }
            }
            else
            {
                address = new Address
                {
                    CountryId = model.NewShippingAddress.CountryId,
                    StateOrProvinceId = model.NewShippingAddress.StateOrProvinceId,
                    DistrictId = model.NewShippingAddress.DistrictId,
                    ZipCode = model.NewShippingAddress.ZipCode,
                    AddressLine1 = model.NewShippingAddress.AddressLine1,
                };
            }

            var orderTaxAndShippingPrice = new OrderTaxAndShippingPriceVm
            {
                Cart = await _cartService.GetActiveCartDetails(cart.CustomerId, cart.CreatedById)
            };

            cart.TaxAmount = orderTaxAndShippingPrice.Cart.TaxAmount = await GetTax(cartId, address.CountryId, address.StateOrProvinceId, address.ZipCode);

            var request = new GetShippingPriceRequest
            {
                OrderAmount = orderTaxAndShippingPrice.Cart.OrderTotal,
                ShippingAddress = address
            };

            orderTaxAndShippingPrice.ShippingPrices = await _shippingPriceService.GetApplicableShippingPrices(request);
            var selectedShippingMethod = string.IsNullOrWhiteSpace(model.SelectedShippingMethodName)
                ? orderTaxAndShippingPrice.ShippingPrices.FirstOrDefault()
                : orderTaxAndShippingPrice.ShippingPrices.FirstOrDefault(x => x.Name == model.SelectedShippingMethodName);
            if (selectedShippingMethod != null)
            {
                cart.ShippingAmount = orderTaxAndShippingPrice.Cart.ShippingAmount = selectedShippingMethod.Price;
                cart.ShippingMethod = orderTaxAndShippingPrice.SelectedShippingMethodName = selectedShippingMethod.Name;
            }

            await _cartRepository.SaveChangesAsync();
            return orderTaxAndShippingPrice;
        }

        private async Task<CouponValidationResult> CheckForDiscountIfAny(Cart cart)
        {
            if (string.IsNullOrWhiteSpace(cart.CouponCode))
            {
                return new CouponValidationResult { Succeeded = true, DiscountAmount = 0 };
            }

            var cartInfoForCoupon = new CartInfoForCoupon
            {
                Items = cart.Items.Select(x => new CartItemForCoupon { ProductId = x.ProductId, Quantity = x.Quantity }).ToList()
            };

            var couponValidationResult = await _couponService.Validate(cart.CustomerId, cart.CouponCode, cartInfoForCoupon);
            return couponValidationResult;
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
        public OrderConf SubeCompra(string laip, string _sesionToken, Cart carro, Address direccion, Address bussines)
        {
            var result = new OrderConf();

            //resultList _area;
            // string token = GetToken(laip).activeToken;
            //string de url principal
            //depurar y revisar alguno de ellos va vacio...
            string urlPath = "https://riews.reinfoempresa.com:8443";
            string nombre = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(carro.Customer.FullName));
            string Comercial = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(bussines.ContactName));
            string calle = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(direccion.AddressLine1));
            string calle2 = string.Empty;
            if (direccion.AddressLine2 != null)
            {
                calle2 = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(direccion.AddressLine2));
            }
            else
            {
                calle2  = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(" "));
            }
            
            string poblacion = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(direccion.City));
            string provincia = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(direccion.StateOrProvinceId.ToString()));
            string cp = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(direccion.ZipCode));
            //cambiar aqui para un campo nif
            string nif = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("44455589q"));
            string pais = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(direccion.CountryId.ToString()));
            string web = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(" "));
            string tfn1 = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(direccion.Phone));
            string tfn2 = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(direccion.Phone));
            string email = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(carro.Customer.Email));
            string pin = "1";
            //string de la url del método de llamada
            //https://riews.reinfoempresa.com:8443/RIEWS/webapi/PrivateServices/articles1
            string request2 = urlPath + "/RIEWS/webapi/PrivateServices/basketlines7W";
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
                string json = "{\"token\":\"" + _sesionToken + "\",\"ipbase64\":\"" + laip + "\",\"name\":\"" + nombre + "\",\"commercialname\":\"" + Comercial + "\",\"address\":\"" + calle + "\",\"address2\":\"" + calle2 + "\",\"population\":\"" + poblacion + "\",\"postalcode\":\"" + cp + "\",\"province\":\"" + provincia + "\"" +
                    ",\"identitydocument\":\"" + nif + "\",\"country\":\"" + pais + "\",\"email\":\"" + email + "\",\"web\":\"" + web + "\",\"phone\":\"" + tfn1 + "\",\"movil\":\"" + tfn2 + "\",\"sendpin\":\""+pin+ "\"}";
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
                result = JsonConvert.DeserializeObject<OrderConf>(result2);
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

        private async Task<Result<ShippingPrice>> ValidateShippingMethod(string shippingMethodName, Address shippingAddress, Cart cart)
        {
            //aqui falla, tenemos que cargar los datos de las lineas
            var applicableShippingPrices = await _shippingPriceService.GetApplicableShippingPrices(new GetShippingPriceRequest
            {
                OrderAmount = cart.Items.Sum(x => x.Product.Price * x.Quantity),
                ShippingAddress = shippingAddress
            });

            var shippingMethod = applicableShippingPrices.FirstOrDefault(x => x.Name == shippingMethodName);
            if (shippingMethod == null)
            {
                return Result.Fail<ShippingPrice>($"Invalid shipping method {shippingMethod}");
            }

            return Result.Ok(shippingMethod);
        }
    }
}
