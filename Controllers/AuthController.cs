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

        
        public AuthController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        //  POST: Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create user
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email
                };

                // FIXED: CreateAsync syntax
                var result = await _userManager.CreateAsync(user, model.Password);

                //  Check success
                if (result.Succeeded)
                {
                    // Create Role if not exists
                    if (!await _roleManager.RoleExistsAsync("User"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("User"));
                    }

                    //  FIXED: Add correct user
                    await _userManager.AddToRoleAsync(user, "User");

                    // Sign in user
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return RedirectToAction("LogIn", "Auth");
                }

                // Show identity errors
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult LogIn()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> LogIn(LogInViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Attempt login using Email and Password
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,        // username (you used email as username)
                    model.Password,
                    isPersistent: false,
                    lockoutOnFailure: false
                );

                // If login successful
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }

                // If login failed
                ModelState.AddModelError("", "Invalid email or password");
            }

            // Return same view with errors
            return View(model);
        }
        //logout
        [HttpGet]
        public async Task <IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}