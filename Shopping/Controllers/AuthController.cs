using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Shopping.Context;
using Shopping.Models;
using Shopping.ViewModels;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Caching.Memory;

namespace Shopping.Controllers
{
    public class AuthController : Controller
    {
        private readonly ShoppingDb _context;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;

        public AuthController(ShoppingDb context, IMemoryCache cache, IConfiguration config)
        {
            _context = context;
            _cache = cache;
            _config = config;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("FirstName,LastName,Email,Password,User Name")] User user)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(user.Email))
                {
                    var userCheck = await _context.Users.FirstOrDefaultAsync(x => x.Email == user.Email || x.UserName == user.UserName);
                    if (userCheck == null)
                    {
                        var salt = BCrypt.Net.BCrypt.GenerateSalt();
                        var newUser = new User
                        {
                            Email = user.Email,
                            LastName = user.LastName,
                            FirstName = user.FirstName,
                            Password = BCrypt.Net.BCrypt.HashPassword(user.Password, salt),
                            UserName = user.UserName
                        };

                        _context.Users.Add(newUser);
                        await _context.SaveChangesAsync();
                        return Redirect("/Auth/Login");
                    }

                    return Content("User  Already Exists");
                }
            }

            return View(user);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var userCheck = _context.Users.FirstOrDefault(x => x.Email == model.Email);
            if (userCheck == null)
            {
                ModelState.AddModelError("Email", "No account with the email is Registered");
                return View(model);
            }

            var hashedPassword = BCrypt.Net.BCrypt.Verify(model.PassWord, userCheck.Password);
            if (hashedPassword)
            {
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userCheck.FirstName + userCheck.LastName),
                new Claim(ClaimTypes.NameIdentifier, userCheck.Id.ToString()),
                new Claim(ClaimTypes.Email, userCheck.Email),
                new Claim(ClaimTypes.Role, userCheck.Role),
            };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                var properties = new AuthenticationProperties(); 

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
                return Redirect("/Products");
            }

            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult RecoveryPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RecoveryPassword(RecoveryPasswordViewModel recoveryPassword)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(recoveryPassword.Email);
            if (!match.Success)
            {
                ModelState.AddModelError("Email", "Email is not valid");
                return View(recoveryPassword);
            }

            var foundUser = _context.Users.FirstOrDefault(x => x.Email == recoveryPassword.Email.Trim());
            if (foundUser == null)
            {
                ModelState.AddModelError("Email", "Email is not exist");
                return View(recoveryPassword);
            }

            var recoverycode = new Random().Next(1000, 10000);
            var email = recoveryPassword.Email;
            var cacheSet = _cache.Set<string>(email, recoverycode.ToString(), TimeSpan.FromMinutes(10));

            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
                mail.From = new MailAddress(_config["Email:Mail"]);
                mail.To.Add(email);
                mail.Subject = "Recovery code";
                mail.Body = "Your recovery code: " + recoverycode;
                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential(_config["Email:Mail"], _config["Email:Mail"]);
                SmtpServer.EnableSsl = true;
                SmtpServer.Send(mail);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error {ex} occurred while sending the recovery code. Please try again.";
                return View(recoveryPassword);
            }

            return Redirect("/Auth/ResetPassword?email=" + foundUser.Email);
        }

        public IActionResult ResetPassword(string email)
        {
            var resetPasswordModel = new ResetPasswordViewModel();
            resetPasswordModel.Email = email;
            return View(resetPasswordModel);
        }

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel resetPassword)
        {
            if (!ModelState.IsValid)
            {
                return View(resetPassword);
            }

            string email = resetPassword.Email;
            _cache.TryGetValue(email, out string? storedCache);

            var foundUser = _context.Users.FirstOrDefault(x => x.Email == resetPassword.Email);
            if (foundUser == null)
            {
                ModelState.AddModelError("Email", "User  not found");
                return View(resetPassword);
            }

            if (storedCache == resetPassword.RecoveryCode)
            {
                bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(resetPassword.NewPassword, foundUser.Password);
                if (isPasswordCorrect)
                {
                    ModelState.AddModelError("NewPassword", "New password can't be the same as old password");
                    return View(resetPassword);
                }

                var salt = BCrypt.Net.BCrypt.GenerateSalt();
                foundUser.Password = BCrypt.Net.BCrypt.HashPassword(resetPassword.NewPassword, salt);
                _context.Users.Update(foundUser);
                _context.SaveChanges();

                return RedirectToAction("Login");
            }

            ModelState.AddModelError("RecoveryCode", "Invalid Recovery Code");
            return View(resetPassword);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(Guid id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
