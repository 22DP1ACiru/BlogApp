using BlogApp.Core.Constants;
using BlogApp.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace BlogApp.DAL.Data
{
    public static class DataSeeder
    {
           public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            // Get required services
            var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            logger.LogInformation("Attempting to seed roles and admin user...");

            // --- Seed Roles ---
            var roleNames = typeof(AppRoles)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                .Select(fi => (string)fi.GetRawConstantValue())
                .ToList();

            if (!roleNames.Any())
            {
                logger.LogWarning("No role constants found in AppRoles class using reflection.");
            }
            else
            {
                logger.LogInformation("Found roles to seed via reflection: {Roles}", string.Join(", ", roleNames));
            }

            foreach (var roleName in roleNames)
            {
                if (string.IsNullOrWhiteSpace(roleName)) continue; // Skip if somehow a constant is empty

                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (roleResult.Succeeded)
                    {
                        logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
                    }
                    else
                    {
                        logger.LogError("Error creating role '{RoleName}'. Errors: {Errors}", roleName, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogDebug("Role '{RoleName}' already exists.", roleName);
                }
            }

            // --- Seed Administrator User ---
            string adminEmail = "admin@blogapp.local"; // CHANGE THIS
            string adminUserName = "AdminUser";       // CHANGE THIS
            string adminPassword = "AdminPassword1!"; // CHANGE THIS (use a strong password!)

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var createAdminResult = await userManager.CreateAsync(adminUser, adminPassword);

                if (createAdminResult.Succeeded)
                {
                    logger.LogInformation("Admin user '{AdminUserName}' created successfully.", adminUserName);
                    var addToRoleResult = await userManager.AddToRoleAsync(adminUser, AppRoles.Administrator);
                    if (addToRoleResult.Succeeded)
                    {
                        logger.LogInformation("Assigned '{RoleName}' role to admin user '{AdminUserName}'.", AppRoles.Administrator, adminUserName);
                    }
                    else
                    {
                        logger.LogError("Error assigning '{RoleName}' role to admin user '{AdminUserName}'. Errors: {Errors}", AppRoles.Administrator, adminUserName, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogError("Error creating admin user '{AdminUserName}'. Errors: {Errors}", adminUserName, string.Join(", ", createAdminResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogDebug("Admin user with email '{AdminEmail}' already exists.", adminEmail);
                if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Administrator))
                {
                    var ensureRoleResult = await userManager.AddToRoleAsync(adminUser, AppRoles.Administrator);
                    if (ensureRoleResult.Succeeded) logger.LogInformation("Ensured admin user '{AdminUserName}' has role '{RoleName}'.", adminUser.UserName, AppRoles.Administrator);
                }
            }
            logger.LogInformation("Finished seeding roles and admin user.");
        }
    }
}