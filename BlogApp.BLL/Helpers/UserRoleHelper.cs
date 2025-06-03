using BlogApp.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace BlogApp.BLL.Helpers
{
    public static class UserRoleHelper
    {
        /// <summary>
        /// Checks if a user is in any of the specified roles.
        /// </summary>
        /// <param name="userManager">The UserManager instance.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="rolesToCheck">An array of role names to check.</param>
        /// <returns>True if the user is in at least one of the specified roles; otherwise, false.</returns>
        public static async Task<bool> IsUserInAnyRoleAsync(
            UserManager<ApplicationUser> userManager,
            string? userId,
            params string[] rolesToCheck)
        {
            if (string.IsNullOrWhiteSpace(userId) || rolesToCheck == null || !rolesToCheck.Any())
            {
                return false;
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            foreach (var roleName in rolesToCheck)
            {
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    continue;
                }

                if (await userManager.IsInRoleAsync(user, roleName))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a user is in a specific role.
        /// Includes null/whitespace checks for user ID and role name.
        /// </summary>
        /// <param name="userManager">The UserManager instance.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="roleName">The name of the role to check.</param>
        /// <returns>True if the user is in the specified role; otherwise, false.</returns>
        public static async Task<bool> IsUserInRoleAsync(
            UserManager<ApplicationUser> userManager,
            string? userId,
            string roleName)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }
            return await userManager.IsInRoleAsync(user, roleName);
        }
    }
}