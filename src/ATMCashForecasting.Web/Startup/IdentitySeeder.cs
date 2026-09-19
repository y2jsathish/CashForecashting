using ATMCashForecasting.Application.Common;
using ATMCashForecasting.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ATMCashForecasting.Web.Startup;

/// <summary>Ensures the six RBAC roles and a break-glass System Administrator account exist on first run.</summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName, Description = $"{roleName} role" });
            }
        }

        var adminUserName = configuration["Seed:AdminUserName"] ?? "admin@atmcash.local";
        var adminPassword = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            // No default password is baked in: without an explicit Seed:AdminPassword (env var, user-secret,
            // or Key Vault entry) the break-glass admin account is simply not created.
            return;
        }

        var existingAdmin = await userManager.FindByNameAsync(adminUserName);
        if (existingAdmin is not null) return;

        var admin = new ApplicationUser
        {
            UserName = adminUserName,
            Email = adminUserName,
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Administrator",
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.SystemAdministrator);
        }
    }
}
