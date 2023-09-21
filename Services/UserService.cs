using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using course_project.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace course_project.Services
{
    public class UserService
    {
        private ApplicationDbContext dbcontext;

        private readonly UserManager<User> userManager;

        private readonly SignInManager<User> signInManager;

        private readonly IUrlHelperFactory urlHelperFactory;

        private readonly IActionContextAccessor actionContextAccessor;
        public UserService(ApplicationDbContext dbcontext, UserManager<User> userManager, SignInManager<User> signInManager, IUrlHelperFactory urlHelperFactory, IActionContextAccessor actionContextAccessor)
        {
            this.dbcontext = dbcontext;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.urlHelperFactory = urlHelperFactory; 
            this.actionContextAccessor = actionContextAccessor;
        }

        public User CreateNewUser(RegisterViewModel model)
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

        public bool CheckNewUserData(RegisterViewModel model)
        {
            return model != null && !string.IsNullOrEmpty(model.Email)
                && !string.IsNullOrEmpty(model.Name) && !string.IsNullOrEmpty(model.Password);
        }

        private async Task CreateAdmin()
        {
            var admin = new User
            {
                UserName = "admin@mail.ru",
                Name = "admin",
                Email = "admin@mail.ru",
                IsBlocked = false
            };
            var adminUserCreated = await userManager.CreateAsync(admin, "admin");
            if (adminUserCreated.Succeeded) await userManager.AddToRoleAsync(admin, "Admin");
        }

        public bool CheckUserLoginData(LogInViewModel model)
        {
            return model != null && !string.IsNullOrEmpty(model.Email)
                    && !string.IsNullOrEmpty(model.Password);
        }

        public async Task<List<User>> GetUsersList(string sorting)
        {
            IQueryable<User> query = dbcontext.Users;
            ParseSortString(sorting, out string sortBy, out string direction);
            switch (sortBy.ToLower())
            {
                case "username":
                    query = direction.ToLower() == "asc" ? query.OrderBy(u => u.UserName) : query.OrderByDescending(u => u.UserName);
                    break;
                case "email":
                    query = direction.ToLower() == "asc" ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email);
                    break;
                case "registrationdate":
                    query = direction.ToLower() == "asc" ? query.OrderBy(u => u.RegistrationDate) : query.OrderByDescending(u => u.RegistrationDate);
                    break;
                default:
                    throw new ArgumentException("Invalid sort by parameter");
            }
            return await query.ToListAsync();
        }

        public void ParseSortString(string sortString, out string sortBy, out string direction)
        {
            var match = Regex.Match(sortString, @"^(.+?)(Asc|Desc)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                sortBy = match.Groups[1].Value;
                direction = match.Groups[2].Value;
            }
            else throw new ArgumentException("Invalid sort string");
        }

        public async Task<IActionResult> DeleteSelectedUsers(SelectedUsersModel model, string currentUserId)
        {
            bool userDeletedSelf = false;
            foreach (var userId in model.selectedUserIds)
            {
                if (userId == currentUserId) userDeletedSelf = true;
                await DeleteUser(userId);
            }
            return userDeletedSelf ? await SignUserOut() : new JsonResult(new { success = true});
        }

        private async Task DeleteUser(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return;
            await userManager.DeleteAsync(user);
            await userManager.UpdateSecurityStampAsync(user);
        }

        private async Task<IActionResult> SignUserOut()
        {
            await signInManager.SignOutAsync();
            var urlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
            return new JsonResult(new { success = true, redirectUrl = urlHelper.Action("Index", "Home") });
        }

        public bool CheckSelectedUsers(SelectedUsersModel model)
        {
            if (model == null || model.selectedUserIds == null || model.selectedUserIds.Count == 0) return false;
            var selectedItem = model.selectedUserIds;
            return selectedItem != null && selectedItem.Count != 0;
        }

        public async Task<IActionResult> UpdateSelectedUsersBlockStatus(SelectedUsersModel model, bool block, string currentUserId)
        {
            bool userBlockedSelf = false;
            foreach (var userId in model.selectedUserIds)
            {
                if (userId == currentUserId) userBlockedSelf = true;
                await UpdateUserBlockStatus(userId.ToString(), block);
            }
            if (block) return userBlockedSelf ? await SignUserOut() : new JsonResult(new { success = true });
            else return new JsonResult(new { success = true });
        }

        private async Task UpdateUserBlockStatus(string userId, bool status)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return;
            user.IsBlocked = status;
            await userManager.UpdateAsync(user);
            await LogUserOut(user);
        }

        private async Task LogUserOut(User user)
        {
            if (user.IsBlocked)
            {
                await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                await userManager.UpdateSecurityStampAsync(user);
            }
            else
            {
                await userManager.SetLockoutEndDateAsync(user, null);
                await userManager.ResetAccessFailedCountAsync(user);
            }
        }
    }
}
