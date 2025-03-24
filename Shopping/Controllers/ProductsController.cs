using Amazon.Internal;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shopping.Context;
using Shopping.External_Services;
using Shopping.Models;
using Shopping.ViewModels;
using System;
using System.IO;
using System.Linq;
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

        // GET: Products/Details/5
        public IActionResult ProductDetails(Guid id)
        {
            var product = _context.Products.FirstOrDefault(x => x.Id == id);
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
                            BucketName = "test-oniline-shop",
                            Key = filePath,
                            ContentType = model.ImageUrl.ContentType,
                            InputStream = memoryStream
                        };
                        var transferUtility = new TransferUtility(amazonClient);
                        await transferUtility.UploadAsync(request);



                        var fileUrl = $"https://test-oniline-shop.s3.eu-west-3.amazonaws.com/{filePath}";

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

        // POST: Products/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Edit(int id, CreateProductViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var product = _context.Products.Find(id);
        //        if (product == null)
        //        {
        //            return NotFound();
        //        }

        //        // Update product data
        //        product.Name = model.Name;
        //        product.Description = model.Description;
        //        product.Discount = model.Discount;
        //        product.Price = model.Price;
        //        product.Quantity = model.Quantity;
        //        product.Tags = model.Tags;

        //        // If there's a new image, upload it to S3 and update the URL
        //        if (model.ImageUrl != null)
        //        {
        //            var fileName = Path.GetFileName(model.ImageUrl.FileName);
        //            var filePath = "products/" + fileName; // Optional: Use a folder structure like 'products/'

        //            var putRequest = new PutObjectRequest
        //            {
        //                BucketName = _awsSettings.BucketName,
        //                Key = filePath,
        //                InputStream = model.ImageUrl.OpenReadStream(),
        //                ContentType = model.ImageUrl.ContentType,
        //                AutoCloseStream = true
        //            };

        //            try
        //            {
        //                var response = _s3Client.PutObjectAsync(putRequest).Result;
        //                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
        //                {
        //                    var fileUrl = $"https://{_awsSettings.BucketName}.s3.{_awsSettings.Region}.amazonaws.com/{filePath}";
        //                    product.ImageUrl = fileUrl; // Update image URL
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                ModelState.AddModelError("", $"Error uploading image: {ex.Message}");
        //                return View(model); // Return back if the image upload fails
        //            }
        //        }

        //        try
        //        {
        //            _context.SaveChanges();
        //            return RedirectToAction(nameof(Index));
        //        }
        //        catch (Exception ex)
        //        {
        //            ModelState.AddModelError("", $"An error occurred while updating the product: {ex.Message}");
        //            return View(model);
        //        }
        //    }
        //    return View(model);
        //}

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
