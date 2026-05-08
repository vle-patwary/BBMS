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
        private readonly AppDbContext _db; // ✅ ADDED

        public AuthController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager,
            AppDbContext db) // ✅ ADDED
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _db = db; // ✅ ADDED
        }

        [HttpGet]
        public IActionResult Register() => View();

        // POST: Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create Identity user
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Create Role if not exists
                    if (!await _roleManager.RoleExistsAsync("User"))
                        await _roleManager.CreateAsync(new IdentityRole("User"));

                    await _userManager.AddToRoleAsync(user, "User");

                    // ✅ Save extra profile data to UserProfiles table
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

                    // Redirect to login after successful registration
                    return RedirectToAction("LogIn", "Auth");
                }

                // Show identity errors
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult LogIn() => View();

        [HttpPost]
        public async Task<IActionResult> LogIn(LogInViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    isPersistent: false,
                    lockoutOnFailure: false
                );

                if (result.Succeeded)
                {
                    // ✅ Redirect based on role
                    var user = await _userManager.FindByEmailAsync(model.Email);

                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                        return RedirectToAction("Admin", "Home");

                    return RedirectToAction("UserDashboard", "Home"); // ✅ Goes to User Dashboard
                }

                ModelState.AddModelError("", "Invalid email or password");
            }

            return View(model);
        }

        // Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}