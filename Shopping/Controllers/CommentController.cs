using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shopping.Context;
using Shopping.Models;
using System.Text.RegularExpressions;

namespace Shopping.Controllers
{
    public class CommentController : Controller
    {
        private readonly ShoppingDb _context;
        public CommentController(ShoppingDb context)
        {
            _context = context;
        }

        // GET: CommentController
        public ActionResult Index()
        {
            return View();
        }

        // GET: CommentController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: CommentController/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CommentController/Create
        [HttpPost]
        public IActionResult Create(string name,string email,string commentText,Guid productId)
        {
            if(!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(email)&&!string.IsNullOrEmpty(commentText) && productId != Guid.NewGuid())
            {
                Regex regex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
                var match = regex.Match(email);
                if(!match.Success)
                {
                    TempData["ErrorMessage"] = "Email is not valid";
                    return Redirect("/Products/ProductDetails/" + productId);
                }
            }
            var comment = new Comment()
            {
                Name = name,
                Email = email,
                CommentText = commentText,
                ProductId = productId,
            };

            _context.Comments.Add(comment);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Comment submitted Successfully";
            return Redirect("/Products/ProductDetails/" + productId);
        }

        // GET: CommentController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: CommentController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Guid id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: CommentController/Delete/5
        public ActionResult Delete(Guid id)
        {
            return View();
        }

        // POST: CommentController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
