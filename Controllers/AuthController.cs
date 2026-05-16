using BBMS.Data;
using BBMS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BBMS.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly AppDbContext _db;

        public AuthController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager,
            AppDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _db = db;
        }

        // ─── USER REGISTER ─────────────────────────────────────────
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Email is already registered.");
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await EnsureRoleExists("User");
                await _userManager.AddToRoleAsync(user, "User");

                var profile = new UserProfile
                {
                    IdentityUserId = user.Id,
                    Name = model.Name,
                    Phone = model.Phone,
                    Address = model.Address,
                    BloodGroup = model.BloodGroup,
                    DonorStatus = "Available"
                };

                _db.UserProfiles.Add(profile);
                await _db.SaveChangesAsync();

                return RedirectToAction("LogIn", "Auth");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // ─── LOGIN ─────────────────────────────────────────────────
        [HttpGet]
        public IActionResult LogIn() => View();

        [HttpPost]
        public async Task<IActionResult> LogIn(LogInViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password,
                isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Admin", "Home");

                if (user != null && await _userManager.IsInRoleAsync(user, "Staff"))
                    return RedirectToAction("Staff", "Home");

                if (user != null && await _userManager.IsInRoleAsync(user, "User"))
                    return RedirectToAction("UserDashboard", "Home");

                // Fallback — shouldn't normally be reached
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // ─── LOGOUT ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ─── HELPER ────────────────────────────────────────────────
        private async Task EnsureRoleExists(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName));
        }
        [HttpGet]
        public IActionResult AccessDenied() => View();
    }
}