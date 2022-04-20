using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Models;
using ParkingSystem.Models.EmailModels;

namespace ParkingSystem.Controllers
{
    public class AccountController : Controller
    {
        UserManager<ApplicationUser> _userManager;
        SignInManager<ApplicationUser> _signInManager;
        RoleManager<IdentityRole> _roleManager;

        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;

        public AccountController(ApplicationDbContext db, UserManager<ApplicationUser> _userManager,
            SignInManager<ApplicationUser> _signInManager, RoleManager<IdentityRole> _roleManager, IEmailSender _emailSender)
        {
            _db = db;
            this._userManager = _userManager;
            this._signInManager = _signInManager;
            this._roleManager = _roleManager;
            this._emailSender = _emailSender;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {

                    var user = await _userManager.FindByNameAsync(model.Email);
                    //var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    string userId = user.Id;
                    //_userManager.GetUserId(User);
                    TempData["UserId"] = userId;
                    IList<string> userRoles = await _userManager.GetRolesAsync(user);
                    if (userRoles.ElementAt(0) == "Admin")
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else if (userRoles.ElementAt(0) == "Customer")
                    {
                        return RedirectToAction("CustomerHomePage", "Customer");
                    }

                }
                ModelState.AddModelError(string.Empty, "Invalid Login attempt!");
            }
            return View(model);
        }
        public async Task<IActionResult> Register()
        {
            if (!_roleManager.RoleExistsAsync(Utility.Helper.Admin).GetAwaiter().GetResult())
            {
                await _roleManager.CreateAsync(new IdentityRole(Utility.Helper.Admin));
                await _roleManager.CreateAsync(new IdentityRole(Utility.Helper.Customer));

            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.RoleName);
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    if (model.RoleName == "Admin")
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    else if (model.RoleName == "Customer")
                    {
                        return RedirectToAction("CustomerHomePage", "Customer");
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logoff()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }


       
    }

   
}
