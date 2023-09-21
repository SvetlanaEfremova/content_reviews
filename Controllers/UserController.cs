using CloudinaryDotNet.Actions;
using course_project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace course_project.Controllers
{
    public class UserController : Controller
    {

        private readonly SignInManager<User> signInManager;

        private readonly UserManager<User> userManager;
        public UserController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult LogIn()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(RegisterViewModel model)
        {
            var admin = new User 
            {
                UserName = "admin@mail.ru", 
                Name = "admin",
                Email = "admin@mail.ru",
                IsBlocked = false
            };
            var adminUserCreated = await userManager.CreateAsync(admin, "admin");

            if (adminUserCreated.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            if (!CheckNewUserData(model)) return RedirectToAction("SignUp", "LogIn");
            var user = CreateNewUser(model);
            var result = await userManager.CreateAsync(user, model.Password);

            return await GetSignUpResult(result, user);
        }

        private bool CheckNewUserData(RegisterViewModel model)
        {
            return model != null && !string.IsNullOrEmpty(model.Email)
                && !string.IsNullOrEmpty(model.Name) && !string.IsNullOrEmpty(model.Password);
        }

        private User CreateNewUser(RegisterViewModel model)
        {
            return new User
            {
                UserName = model.Email,
                Name = model.Name,
                Email = model.Email,
                IsBlocked = false,
                RegistrationDate = DateTime.Now,
            };
        }

        private async Task<IActionResult> GetSignUpResult(IdentityResult result, User user)
        {
            if (result.Succeeded)
            {
                await signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index","Home");
            }
            else return RedirectToAction("SignUp", "User");
        }

        [HttpPost]
        public async Task<IActionResult> LogIn(LogInViewModel model)
        {
            if (!CheckUserLoginData(model)) return RedirectToAction("LogIn", "User");
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction("LogIn", "User");
            else return await LoginUser(user, model);
        }

        private bool CheckUserLoginData(LogInViewModel model)
        {
            return model != null && !string.IsNullOrEmpty(model.Email)
                    && !string.IsNullOrEmpty(model.Password);
        }

        private async Task<IActionResult> LoginUser(User user, LogInViewModel model)
        {
            if (user.IsBlocked) return RedirectToAction("LogIn", "User");
            var result = await signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: false);
            return await HandleLogInResult(result, user);
        }

        private async Task<IActionResult> HandleLogInResult(Microsoft.AspNetCore.Identity.SignInResult result, User user)
        {
            if (result.Succeeded)
            {
                await userManager.UpdateAsync(user);
                return RedirectToAction("Index", "Home");
            }
            else return RedirectToAction("LogIn", "User");
        }

        public async Task<IActionResult> LogOut()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "User", new { ReturnUrl = returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            var errorResult = HandleExternalLoginError(remoteError);
            if (errorResult != null) return errorResult;
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null) return RedirectToAction("LogIn", "User");
            return await HandleExternalLoginResult(returnUrl, info);
        }

        private IActionResult HandleExternalLoginError(string remoteError)
        {
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
                return RedirectToAction("LogIn", "User");
            }
            return null;
        }

        private async Task<IActionResult> HandleExternalLoginResult(string returnUrl, ExternalLoginInfo info)
        {
            var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            return result.Succeeded ? RedirectToLocal(returnUrl) : await TryToLoginExternalUser(returnUrl, info);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            else
                return RedirectToAction("Index", "Home");
        }

        private async Task<IActionResult> TryToLoginExternalUser(string returnUrl, ExternalLoginInfo info)
        {
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email != null) return await LogInExternalUser(returnUrl, email, info);
            else return RedirectToAction("LogIn", "User");
        }

        private async Task<IActionResult> LogInExternalUser(string returnUrl, string email, ExternalLoginInfo info)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null) user = await CreateNewExternalUser(email, info);
            else await userManager.AddLoginAsync(user, info);
            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToLocal(returnUrl);
        }

        private async Task<User> CreateNewExternalUser(string email, ExternalLoginInfo info)
        {
            var user = new User { UserName = email, Email = email, IsBlocked = false, Name = email };
            var createUserResult = await userManager.CreateAsync(user);
            if (createUserResult.Succeeded) await userManager.AddLoginAsync(user, info);
            return user;
        }

    }
}
