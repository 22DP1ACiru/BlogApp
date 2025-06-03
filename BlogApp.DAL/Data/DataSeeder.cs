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
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(nameof(DataSeeder));
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            logger.LogInformation("Starting data seeding process for roles and admin user...");

            // --- Seed Roles ---
            // Get all public static string constants from AppRoles class
            var roleNames = typeof(AppRoles)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                .Select(fi => (string?)fi.GetRawConstantValue())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            if (!roleNames.Any())
            {
                logger.LogWarning("No role constants found in AppRoles class using reflection, or all were empty.");
            }
            else
            {
                logger.LogInformation("Found roles to seed via reflection: {Roles}", string.Join(", ", roleNames));
            }

            foreach (var roleName in roleNames)
            {
                if (string.IsNullOrWhiteSpace(roleName)) continue;

                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (roleResult.Succeeded)
                    {
                        logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
                    }
                    else
                    {
                        logger.LogError("Error creating role '{RoleName}'. Errors: {Errors}",
                            roleName, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogDebug("Role '{RoleName}' already exists. Skipping creation.", roleName);
                }
            }

            // --- Seed Administrator User ---
            const string adminEmail = "admin@blogapp.local"; // IMPORTANT: Change this in a real application
            const string adminUserName = "AdminUser";       // IMPORTANT: Change this
            const string adminPassword = "AdminPassword1!"; // IMPORTANT: Change to a STRONG, unique password and manage securely

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
                    logger.LogInformation("Admin user '{AdminUserName}' created successfully with email '{AdminEmail}'.", adminUserName, adminEmail);

                    // Assign Administrator role
                    if (!string.IsNullOrWhiteSpace(AppRoles.Administrator))
                    {
                        if (await roleManager.RoleExistsAsync(AppRoles.Administrator))
                        {
                            var addToRoleResult = await userManager.AddToRoleAsync(adminUser, AppRoles.Administrator);
                            if (addToRoleResult.Succeeded)
                            {
                                logger.LogInformation("Assigned '{RoleName}' role to admin user '{AdminUserName}'.", AppRoles.Administrator, adminUserName);
                            }
                            else
                            {
                                logger.LogError("Error assigning '{RoleName}' role to admin user '{AdminUserName}'. Errors: {Errors}",
                                    AppRoles.Administrator, adminUserName, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
                            }
                        }
                        else
                        {
                            logger.LogWarning("Administrator role '{RoleName}' does not exist. Cannot assign to admin user. Ensure roles are seeded first.", AppRoles.Administrator);
                        }
                    }
                    else
                    {
                        logger.LogWarning("AppRoles.Administrator constant is empty. Cannot assign to admin user.");
                    }
                }
                else
                {
                    logger.LogError("Error creating admin user '{AdminUserName}'. Errors: {Errors}",
                        adminUserName, string.Join(", ", createAdminResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogDebug("Admin user with email '{AdminEmail}' already exists.", adminEmail);
                // Ensure existing admin user has the Administrator role
                if (!string.IsNullOrWhiteSpace(AppRoles.Administrator) && await roleManager.RoleExistsAsync(AppRoles.Administrator))
                {
                    if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Administrator))
                    {
                        var ensureRoleResult = await userManager.AddToRoleAsync(adminUser, AppRoles.Administrator);
                        if (ensureRoleResult.Succeeded)
                        {
                            logger.LogInformation("Ensured existing admin user '{AdminUserName}' has role '{RoleName}'.", adminUser.UserName, AppRoles.Administrator);
                        }
                        else
                        {
                            logger.LogError("Failed to ensure existing admin user '{AdminUserName}' has role '{RoleName}'. Errors: {Errors}",
                                adminUser.UserName, AppRoles.Administrator, string.Join(", ", ensureRoleResult.Errors.Select(e => e.Description)));
                        }
                    }
                }
                else if (string.IsNullOrWhiteSpace(AppRoles.Administrator))
                {
                    logger.LogWarning("AppRoles.Administrator constant is empty. Cannot verify role for existing admin user.");
                }
                else if (!await roleManager.RoleExistsAsync(AppRoles.Administrator))
                {
                    logger.LogWarning("Administrator role '{RoleName}' does not exist. Cannot verify role for existing admin user.", AppRoles.Administrator);
                }
            }
            logger.LogInformation("Data seeding process completed.");
        }
    }
}