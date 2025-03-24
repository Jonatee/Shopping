using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shopping.Context;
using System.Security.Claims;

namespace Shopping.Controllers
{
    [Area("User")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ShoppingDb _context;

        public OrderController(ShoppingDb context)
        {
            _context = context;
        }

        // GET: User/Orders
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userId, out Guid userIdGuid);

            var result = await _context.Orders.Where(x => x.UserId == userIdGuid).OrderByDescending(x => x.Id).ToListAsync();
            return View(result);
        }

        // GET: User/Orders/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userId, out Guid userIdGuid);

            var order = await _context.Orders
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userIdGuid);
            if (order == null)
            {
                return NotFound();
            }

            ViewData["OrderDetails"] = await _context.OrderDetails.
                                        Where(x => x.OrderId == id).ToListAsync();

            return View(order);
        }

        // GET: User/Orders/Create


        private bool OrderExists(Guid id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
