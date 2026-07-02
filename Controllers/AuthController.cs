using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using SyncSyntax.Models.ViewModels;

namespace SyncSyntax.Controllers
{
    public class AuthController : Controller

    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        //Register
        //Login
        //Logout

        public AuthController(UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole>roleManager,
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

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            //cheak validation
            if (ModelState.IsValid)

            {
                //Cteate Identity User Object 
                var user = new IdentityUser { UserName = model.Email, Email = model.Email };

                //USER CREATE 
                var result = await _userManager.CreateAsync(user, model.Password);  
                
                 //If User Ceated Sucessfully 
                 if (result.Succeeded)
                {
                    //If the user Role exists in data base
                    if(!await _roleManager.RoleExistsAsync("User"))
                    {

                       await _roleManager.CreateAsync(new IdentityRole("User"));
                    }

                     await  _userManager.AddToRoleAsync(user, "User");

                    await _signInManager.SignInAsync(user, true);

                    return RedirectToAction("Index", "Post");   
                }
            }


            return View(model);
        }

        [HttpGet]
        public IActionResult Login()    
        {
            return View();
        }
        [HttpPost]

        public async Task <IActionResult>Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("", "User not found");
                    return View(model);
                }
                 

                var signInresult  =  await _signInManager.PasswordSignInAsync(user,model.Password, false, false);
                    
                if (!signInresult.Succeeded)
                {
                    ModelState.AddModelError(" ", "Email or Password is Incorrect ");
                    return View(model);
                   
                }
                return RedirectToAction("Index", "Post");
            }

            return View(model);
        }

        [HttpGet]

        public async Task <IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index","Post");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}

   

