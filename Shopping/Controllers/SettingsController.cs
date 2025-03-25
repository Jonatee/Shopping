using Amazon.S3.Transfer;
using Amazon.S3;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shopping.Context;
using Shopping.Models;
using Microsoft.AspNetCore.Authorization;

namespace Shopping.Controllers
{
    [Authorize(Roles ="Admin")]
    public class SettingsController : Controller
    {
        private readonly ShoppingDb _context;
        private readonly IConfiguration _config;

        public SettingsController(ShoppingDb context ,IConfiguration configuration)
        {
            _context = context;
            _config = configuration;
        }



        // GET: Admin/Settings/Edit/5
        public async Task<IActionResult> Edit()
        {

            var setting = await _context.Settings.FirstAsync();
            if (setting == null)
            {
                return NotFound();
            }
            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id,
            [Bind("Id,Shipping,Title,Address,Email,Phone,CopyRight,Instagram,FaceBook,GooglePlus,Youtube,Twitter,Logo")]
                 Setting setting, IFormFile? newLogo)
        {
            if (id != setting.Id)
            {
                return NotFound();
            }

            if (setting != null)
            {
                var urltodatabase = "";
                if (newLogo != null)

                {
                    var fileName = Path.GetFileName(newLogo.FileName);
                    var filePath = "settingslogo/" + fileName;
                    string accessKey = _config["AWSsettings:AccessKey"];
                    string secretKey = _config["AWSsettings:SecretKey"];
                    string bucketName = _config["AWSsettings:BucketName"];

                    using (var amazonClient = new AmazonS3Client(accessKey, secretKey, Amazon.RegionEndpoint.EUWest3))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            newLogo.CopyTo(memoryStream);
                            var request = new TransferUtilityUploadRequest
                            {
                                BucketName = bucketName,
                                Key = filePath,
                                ContentType = newLogo.ContentType,
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
                if (ModelState.IsValid)
                {
                    try
                    {
                        setting.Logo = urltodatabase;
                        _context.Update(setting);
                        await _context.SaveChangesAsync();

                        TempData["message"] = "Setting saved";
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!SettingExists(setting.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                return Redirect("/Settings/Edit");
            }
            return View();
        }
        


        private bool SettingExists(Guid id)
        {
            return _context.Settings.Any(e => e.Id == id);
        }

    }
}

