using ChatBoard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
namespace ChatBoard.Controllers
{
    public class AccountController : Controller
    {
        private readonly ChatBoardContext _context;
        public AccountController(ChatBoardContext context)
        {
            _context = context;
        }
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Register(User user)
        {
            ModelState.Remove(nameof(user.PasswordHash));
            ModelState.Remove(nameof(user.ProfileImage));
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email is already registered");
                return View(user);
            }
            if (ModelState.IsValid)
            {
                user.PasswordHash = HashPassword(user.Password);
                user.ProfileImage = "/images/default-user.png";
                _context.Users.Add(user);
                _context.SaveChanges();
                return RedirectToAction("Login");
            }
            return View(user);
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(string empcode, string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.EmpCode == empcode);
            if (user != null && VerifyPassword(password, user.PasswordHash))
            {
                // Store user info in session or cookie
                HttpContext.Session.SetInt32("UserId", user.UserId);
                return RedirectToAction("Index", "Chat");
            }
            ViewBag.Error = "Invalid credentials";
            return View();
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        private bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}