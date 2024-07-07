using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalManagement.Models;
using RentalManagement.ViewModels;
using System.Security.Claims;

[Authorize]
public class AccountController : Controller
{
    private readonly MainContext _context;

    public AccountController(MainContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "帳號清單";

        var properties = await _context.SYS_User.Where(u => u.DeleteAt == null).ToListAsync();

        return View(properties);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Register()
    {
        ViewData["title"] = "註冊帳號";
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Register(UserViewModel request)
    {
        ViewData["title"] = "註冊帳號";

        if (ModelState.IsValid)
        {
            bool check = await _context.SYS_User.AnyAsync(u => u.Username == request.Username);

            if (check)
            {
                TempData["Fail"] = request.Username + "帳號已存在";
                return View(request);
            }
            var user = new SYS_User
            {
                Username = request.Username,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                DisplayName = request.DisplayName,
                Enable = true,
                NotValidBefore = new DateTime(DateTime.MinValue.Ticks),
                NotValidAfter = new DateTime(DateTime.MaxValue.Ticks)
            };
            var res = await _context.SYS_User.AddAsync(user);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }
        TempData["Fail"] = "Registration Fail!";
        return View(request);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Login()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(UserViewModel request)
    {
        if (ModelState.IsValid)
        {
            bool check = await _context.SYS_User.AnyAsync(u => u.Username == request.Username);
            if (check)
            {
                SYS_User user = _context.SYS_User
                    .Where(u => u.Username == request.Username)
                    .FirstOrDefault();
                if (BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                {
                    var claims = new List<Claim> { new Claim(ClaimTypes.Name, request.Username) };
                    ClaimsIdentity userIdentity = new ClaimsIdentity(claims, "login");
                    ClaimsPrincipal principal = new ClaimsPrincipal(userIdentity);

                    await HttpContext.SignInAsync(principal);
                    return RedirectToAction("Index", "Home");
                }
            }
        }
        TempData["Fail"] = "帳號或密碼錯誤";
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
