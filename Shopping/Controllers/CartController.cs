using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using PayPal.Api;
using Shopping.Configurations;
using Shopping.Context;
using Shopping.Models;
using Shopping.ViewModels;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Shopping.Controllers
{
    public class CartController : Controller
    {
        private readonly ShoppingDb _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
         private readonly IConfiguration _configuration;
        public CartController(ShoppingDb context, IHttpContextAccessor httpContextAccessor, IConfiguration iconfiguration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = iconfiguration;
            _context = context;
        }
        public IActionResult Index()
        {
            var result = GetProductsinCart();
            return View(result);
        }
        public IActionResult ClearCart()
        {
            Response.Cookies.Delete("Cart");
            return Redirect("/");
        }
        /// <summary>
        /// Add or update the shopping cart
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="count">
        ///  If quantity is zero, it means the intention is to remove the item. 
        /// This case is manually handled by us.
        /// </param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult UpdateCart([FromBody] CartViewModel request)
        {

            var product = _context.Products.FirstOrDefault(x => x.Id == request.ProductId);
            if (product == null)
            {
                return NotFound();
            }
            if (product.Quantity < request.Count)
            {
                return BadRequest();
            }
            // Retrieve the list of products in the cart using the dedicated function
            var cartItems = GetCartItems();

            var foundProductInCart = cartItems.FirstOrDefault(x => x.ProductId == request.ProductId);

            // If the product is found, it means it is in the cart, and the user intends to change the quantity
            if (foundProductInCart == null)
            {
                var newCartItem = new CartViewModel() { };
                newCartItem.ProductId = request.ProductId;
                newCartItem.Count = request.Count;

                cartItems.Add(newCartItem);
            }
            else
            {
                // If greater than zero, it means the user wants to update the quantity; otherwise, it will be removed from the cart.
                if (request.Count > 0)
                {
                    foundProductInCart.Count = request.Count;
                }
                else
                {
                    cartItems.Remove(foundProductInCart);
                }
            }

            var json = JsonConvert.SerializeObject(cartItems);

            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddDays(7);
            Response.Cookies.Append("Cart", json, option);

            var result = cartItems.Sum(x => x.Count);

            return Ok(result);
        }

        public List<CartViewModel> GetCartItems()
        {
            List<CartViewModel> cartList = new List<CartViewModel>();

            var prevCartItemsString = Request.Cookies["Cart"];

            // If it's not null, it means the cart is not empty, so we need to convert it to a list of view models; 
            // otherwise, we return an empty cart list.
            if (!string.IsNullOrEmpty(prevCartItemsString))
            {
                cartList = JsonConvert.DeserializeObject<List<CartViewModel>>(prevCartItemsString);
            }

            return cartList;
        }

        public List<ProductCartViewModel> GetProductsinCart()
        {
            var cartItems = GetCartItems();

            if (!cartItems.Any())
            {
                return null;
            }

            var cartItemProductIds = cartItems.Select(x => x.ProductId).ToList();
            // Load products into memory
            var products = _context.Products
                .Where(p => cartItemProductIds.Contains(p.Id))
                .ToList();

            // Create the ProductCartViewModel list

            List<ProductCartViewModel> result = new List<ProductCartViewModel>();
            foreach (var item in products)
            {
                var newItem = new ProductCartViewModel
                {
                    Id = item.Id,
                    ImageName = item.ImageUrl,
                    Price = item.Price - (item.Discount ?? 0),
                    Title = item.Name,
                    Count = cartItems.Single(x => x.ProductId == item.Id).Count,
                    RowSumPrice = (item.Price - (item.Discount ?? 0)) * cartItems.Single(x => x.ProductId == item.Id).Count,
                };

                result.Add(newItem);
            }

            return result;
        }
        public IActionResult SmallCart()
        {
            var result = GetProductsinCart();
            return PartialView(result);
        }

        [Authorize]
        public IActionResult Checkout()
        {
            var order = new Models.Order();

            var shipping = _context.Settings.First().Shipping;
            if (shipping != null)
            {
                order.Shipping = shipping;
            }

            ViewData["Products"] = GetProductsinCart();
            return View(order);
        }
        [Authorize]
        [HttpPost]
        public IActionResult ApplyCouponCode([FromForm] string couponCode)
        {
            var order = new Models.Order();

            var coupon = _context.Coupons.FirstOrDefault(c => c.Code == couponCode);

            if (coupon != null)
            {
                order.CouponCode = coupon.Code;
                order.CouponDiscount = coupon.Discount;
            }
            else
            {
                ViewData["Products"] = GetProductsinCart();
                TempData["message"] = "Coupon not exist";
                return View("Checkout", order);
            }

            var shipping = _context.Settings.First().Shipping;
            if (shipping != null)
            {
                order.Shipping = shipping;
            }

            ViewData["Products"] = GetProductsinCart();
            return View("Checkout", order);
        }

        [Authorize]
        [HttpPost]
        public IActionResult Checkout(Models.Order order)
        {
            if (!ModelState.IsValid)
            {

                ViewData["Products"] = GetProductsinCart();

                return View(order);
            }

            //-------------------------------------------------------

            //check and find coupon
            if (!string.IsNullOrEmpty(order.CouponCode))
            {
                var coupon = _context.Coupons.FirstOrDefault(c => c.Code == order.CouponCode);

                if (coupon != null)
                {
                    order.CouponCode = coupon.Code;
                    order.CouponDiscount = coupon.Discount;
                }
                else
                {
                    TempData["message"] = "Coupon not exist";
                    ViewData["Products"] = GetProductsinCart();

                    return View(order);
                }
            }

            var products = GetProductsinCart();
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userGuid);

            order.Shipping = _context.Settings.First().Shipping;
            order.SubTotal = products.Sum(x => x.RowSumPrice);
            order.Total = (order.SubTotal + order.Shipping ?? 0);
            order.UserId = userGuid;

            if (order.CouponDiscount != null)
            {
                order.Total -= order.CouponDiscount;
            }

            _context.Orders.Add(order);
            _context.SaveChanges();

            //-------------------------------------------------------

            List<OrderDetail> orderDetails = new List<OrderDetail>();

            foreach (var item in products)
            {
                OrderDetail orderDetailItem = new OrderDetail()
                {
                    Count = item.Count,
                    ProductTitle = item.Title,
                    ProductPrice = (decimal)item.Price,
                    OrderId = order.Id,
                    ProductId = item.Id
                };

                orderDetails.Add(orderDetailItem);
            }
            //-------------------------------------------------------
            _context.OrderDetails.AddRange(orderDetails);
            _context.SaveChanges();

            // Redirect to PayPal
            return Redirect("/Cart/RedirectToPayPal?orderId=" + order.Id);
        }

        public ActionResult RedirectToPayPal(Guid orderId)
        {
            var order = _context.Orders.Find(orderId);
            if (order == null)
            {
                return View("PaymentFailed");
            }

            var orderDetails = _context.OrderDetails.Where(x => x.OrderId == orderId).ToList();

            var clientId = _configuration.GetValue<string>("PayPal:Key");
            var clientSecret = _configuration.GetValue<string>("PayPal:Secret");
            var mode = _configuration.GetValue<string>("PayPal:mode");
            var apiContext = PaypalConfiguration.GetAPIContext(clientId, clientSecret, mode);

            try
            {
                string baseURI = $"{Request.Scheme}://{Request.Host}/cart/PaypalReturn?";
                var guid = Guid.NewGuid().ToString();

                var payment = new Payment
                {
                    intent = "sale",
                    payer = new Payer { payment_method = "paypal" },
                    transactions = new List<Transaction>
            {
                new Transaction
                {
                    description = $"Order {order.Id}",
                    invoice_number = Guid.NewGuid().ToString(),
                    amount = new Amount
                    {
                        currency = "USD",
                        total = order.Total?.ToString("F"),
                    },

                
                }
            },
                    redirect_urls = new RedirectUrls
                    {
                        cancel_url = $"{baseURI}&Cancel=true",
                        return_url = $"{baseURI}orderId={order.Id}"
                    }
                };
              

                var createdPayment = payment.Create(apiContext);
                var approvalUrl = createdPayment.links.FirstOrDefault(l => l.rel.ToLower() == "approval_url")?.href;

                _httpContextAccessor.HttpContext.Session.SetString("payment", createdPayment.id);
                return Redirect(approvalUrl);
            }
            catch (Exception e)
            {
                return View("PaymentFailed");
            }
        }

        public ActionResult PaypalReturn(Guid orderId, string PayerID)
        {
            var order = _context.Orders.Find(orderId);
            if (order == null)
            {
                return View("PaymentFailed");
            }

            var clientId = _configuration.GetValue<string>("PayPal:Key");
            var clientSecret = _configuration.GetValue<string>("PayPal:Secret");
            var mode = _configuration.GetValue<string>("PayPal:mode");
            var apiContext = PaypalConfiguration.GetAPIContext(clientId, clientSecret, mode);

            try
            {
                var paymentId = _httpContextAccessor.HttpContext.Session.GetString("payment");
                var paymentExecution = new PaymentExecution { payer_id = PayerID };
                var payment = new Payment { id = paymentId };

                var executedPayment = payment.Execute(apiContext, paymentExecution);

                if (executedPayment.state.ToLower() != "approved")
                {
                    return View("PaymentFailed");
                }

                Response.Cookies.Delete("Cart");
                // Save the PayPal transaction ID and update order status
                order.TransId = executedPayment.transactions[0].related_resources[0].sale.id;
                order.Status = executedPayment.state.ToLower();
                //---------Reduce QTY-------------
                var orderDetails = _context.OrderDetails.Where(x => x.OrderId == orderId).ToList();

                var productsIds = orderDetails.Select(x => x.ProductId);

                var products = _context.Products.Where(x => productsIds.Contains(x.Id)).ToList();

                foreach (var item in products)
                {
                    item.Quantity = item.Quantity - orderDetails.FirstOrDefault(x => x.ProductId == item.Id).Count;
                }

                _context.Products.UpdateRange(products);
                //---------------------------------
                _context.SaveChanges();

                ViewData["orderId"] = order.Id;

                return View("PaymentSuccess");
            }
            catch (Exception)
            {
                return View("PaymentFailed");
            }
        }

    }
}

