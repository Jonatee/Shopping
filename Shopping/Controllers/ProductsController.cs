using Amazon.Internal;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shopping.Context;
using Shopping.External_Services;
using Shopping.Models;
using Shopping.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shopping.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ShoppingDb _context;
        private readonly IConfiguration _config;

        public ProductsController(ShoppingDb context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: Products
        public IActionResult Index()
        {
            var getallproducts = _context.Products.OrderByDescending(x => x.CreatedOn);
            if(getallproducts.Count() == 0)
            {
                return View();
            }
            return View(getallproducts);

        }
        public IActionResult SearchProducts(string SearchText)
        {
            var products = _context.Products
                 .Where(x =>
                 EF.Functions.Like(x.Name, "%" + SearchText + "%") ||
                 EF.Functions.Like(x.Tags, "%" + SearchText + "%")
                 )
                 .OrderBy(x => x.Name)
                 .ToList();
            return View("Index", products);
        }
        [HttpPost]
        public IActionResult SubmitComment(string name, string email, string comment, Guid productId)
        {
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(comment))
            {
                Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
                Match match = regex.Match(email);
                if (!match.Success)
                {
                    TempData["ErrorMessage"] = "Email is not valid";
                    return Redirect("/Products/ProductDetails/" + productId);
                }
                var newComment = new Comment()
                {
                    CommentText = comment,
                    Name = name,
                    ProductId = productId,
                    Email = email
                };
                _context.Comments.Add(newComment);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Your comment submited successfully";
                return Redirect("/Products/ProductDetails/" + productId);
            }
            else
            {
                TempData["ErrorMessage"] = "Please complete your information";
                return Redirect("/Products/ProductDetails/" + productId);
            }

        }

        // GET: Products/Details/5
        public IActionResult ProductDetails(Guid id)
        {
            var product = _context.Products.FirstOrDefault(x => x.Id == id);
            ViewData["banners"] = _context.Banners.ToList();
            ViewData["comments"] = _context.Comments.Where(x => x.ProductId == id).ToList();
            return View(product);
        }

        // GET: Products/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateProductViewModel model)
        {
            string accessKey = _config["AWSsettings:AccessKey"];
            string secretKey = _config["AWSsettings:SecretKey"];
            string bucketName = _config["AWSsettings:BucketName"];
            var urltodatabase = "";
            if (model.ImageUrl != null)

            {
                var fileName = Path.GetFileName(model.ImageUrl.FileName);
                var filePath = "products/" + fileName;

                using (var amazonClient = new AmazonS3Client(accessKey, secretKey, Amazon.RegionEndpoint.EUWest3))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        model.ImageUrl.CopyTo(memoryStream);
                        var request = new TransferUtilityUploadRequest
                        {
                            BucketName = bucketName,
                            Key = filePath,
                            ContentType = model.ImageUrl.ContentType,
                            InputStream = memoryStream,
                            CannedACL = S3CannedACL.PublicRead
                        };
                        var transferUtility = new TransferUtility(amazonClient);
                        await transferUtility.UploadAsync(request);



                        string awsPath = _config["AWSS3BUCKET:Path"];

                        var fileUrl = $"{awsPath}/{filePath}";

                        // Store the URL in the model or database
                        urltodatabase = fileUrl;

                    }
                }
            }
            try
            {
                // Save product data to the database (including the image URL)
                var newProduct = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    Discount = model.Discount,
                    Price = model.Price,
                    Quantity = model.Quantity,
                    Tags = model.Tags,
                    ImageUrl = urltodatabase // Store the image URL from S3
                };

                _context.Products.Add(newProduct);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index"); // Redirect to the product list page
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while saving the product: {ex.Message}");
                return View(model); // Return back with error if saving fails
            }

            }




        // GET: Products/Edit/5
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

     

        // GET: Products/Delete/5
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Products/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            try
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while deleting the product: {ex.Message}");
                return View(product);
            }
        }
    }
}
