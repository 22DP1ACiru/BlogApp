using BlogApp.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace BlogApp.BLL.Helpers
{
    public static class UserRoleHelper
    {
        /// <summary>
        /// Checks if a user is in a list of specified roles.
        /// </summary>
        /// <param name="userManager">The UserManager instance.</param>
        /// <param name="userId">The ID of the user to check.</param>
        /// <param name="rolesToCheck">An array of role names to check against.</param>
        /// <returns>True if the user is in at least one of the specified roles, false otherwise.</returns>
        public static async Task<bool> IsUserInAnyRoleAsync(
            UserManager<ApplicationUser> userManager,
            string? userId,
            params string[] rolesToCheck)
        {
            // Null/empty array checks
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
                if (string.IsNullOrWhiteSpace(roleName)) continue; // Skip empty role names

                if (await userManager.IsInRoleAsync(user, roleName))
                {
                    return true; // User found in one of the required roles
                }
            }

            return false; // User not found in any of the specified roles
        }

        /// <summary>
        /// Checks if a user is in a specific role. Wraps IsInRoleAsync but with null checks.
        /// </summary>
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