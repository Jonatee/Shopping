using Amazon.S3.Transfer;
using Amazon.S3;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shopping.Context;
using Shopping.Models;
using Shopping.ViewModels;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace Shopping.Controllers
{
    public class BannerController : Controller
    {
        private readonly ShoppingDb _context;
        private readonly IConfiguration _config;

        public BannerController(ShoppingDb context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }


        // GET: BannerController
        public  IActionResult Index()
        {
            var allbanners = _context.Banners.ToList();
            return View(allbanners);
        }

        // GET: BannerController/Details/5
        public IActionResult BannerDetails(Guid id)
        {
            var banner = _context.Banners.FirstOrDefault(a => a.Id == id);
            return View(banner);
        }

        // GET: BannerController/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }
        [Authorize(Roles="Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateBannerViewModel model)
        {
            if (model != null)
            {
                var urltodatabase = "";
                if (model.ImageUrl != null)

                {
                    var fileName = Path.GetFileName(model.ImageUrl.FileName);
                    var filePath = "banners/" + fileName;
                    string accessKey = _config["AWSsettings:AccessKey"];
                    string secretKey = _config["AWSsettings:SecretKey"];
                    string bucketName = _config["AWSsettings:BucketName"];

                    using (var amazonClient = new AmazonS3Client(accessKey, secretKey, Amazon.RegionEndpoint.EUWest3))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            model.ImageUrl.CopyTo(memoryStream);
                            var request = new TransferUtilityUploadRequest
                            {
                                BucketName =bucketName,
                                Key = filePath,
                                ContentType = model.ImageUrl.ContentType,
                                InputStream = memoryStream,
                                CannedACL = S3CannedACL.PublicRead
                            };
                            var transferUtility = new TransferUtility(amazonClient);
                            await transferUtility.UploadAsync(request);

                            string awsPath = _config["AWSS3BUCKET:Path"];

                            var fileUrl = $"{awsPath}/{filePath}";


                            urltodatabase = fileUrl;

                        }
                    }
                }
                try
                {
                    var newBanner = new Banner
                    {
                        SubTitle = model.SubTitle,
                        Title = model.Title,
                        Link = model.Link,
                        ImageUrl = urltodatabase
                    };

                    _context.Banners.Add(newBanner);

                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Link", $"Erroor occurred while saving the product: {ex.Message}");
                    return View(model);
                }
            }
            return View(model);
        }

        // GET: BannerController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: BannerController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
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

        // GET: BannerController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: BannerController/Delete/5
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
