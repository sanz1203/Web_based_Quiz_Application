using Microsoft.AspNetCore.Mvc;
using Quiz_Application.Models.ViewModels;
using Quiz_Application.Data;
using Quiz_Application.Models.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Quiz_Application.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext dbContext;

        public AccountController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Register() => View();


        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (dbContext.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("", "User already exists.");
                    return View(model);
                }

                var user = new User
                {
                    Email = model.Email,
                    PasswordHash = ComputeHash(model.Password),
                    IsAdmin = false
                };

                dbContext.Users.Add(user);
                dbContext.SaveChanges();

                HttpContext.Session.SetString("IsAdmin", "false");
                await SignInUser(user.Email, false);
                return RedirectToAction("All", "Quiz");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = dbContext.Users.FirstOrDefault(u => u.Email == model.Email);

                if (user != null && user.PasswordHash == ComputeHash(model.Password))
                {
                    HttpContext.Session.SetString("IsAdmin", user.IsAdmin ? "true" : "false");
                    await SignInUser(user.Email, user.IsAdmin);

                    if (user.IsAdmin)
                        return RedirectToAction("Index", "Admin");
                    else
                        return RedirectToAction("All", "Quiz");
                }

                ModelState.AddModelError("", "Invalid credentials.");
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Remove("IsAdmin");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        private async Task SignInUser(string email, bool isAdmin)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email)
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        private string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
