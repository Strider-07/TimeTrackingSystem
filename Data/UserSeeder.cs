using Microsoft.AspNetCore.Identity;
using TimeTrackingSystem.Models.Entities;

namespace TimeTrackingSystem.Data
{
    public static class UserSeeder
    {
        public static async Task SeedAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            string[] roles =
            {
            "Administrator",
            "Kierownik",
            "Pracownik"
        };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }

            var users = new[]
            {
            new ApplicationUser
            {
                FirstName = "Adam",
                LastName = "Kowalski",
                UserName = "adam.kowalski@techcorp.local",
                Email = "adam.kowalski@techcorp.local",
                Position = "Administrator systemu",
                Department = "IT",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ApplicationUser
            {
                FirstName = "Anna",
                LastName = "Nowak",
                UserName = "anna.nowak@techcorp.local",
                Email = "anna.nowak@techcorp.local",
                Position = "Team Leader",
                Department = "IT",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ApplicationUser
            {
                FirstName = "Piotr",
                LastName = "Wiśniewski",
                UserName = "piotr.wisniewski@techcorp.local",
                Email = "piotr.wisniewski@techcorp.local",
                Position = "Backend Developer",
                Department = "IT",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new ApplicationUser
            {
                FirstName = "Marta",
                LastName = "Zielińska",
                UserName = "marta.zielinska@techcorp.local",
                Email = "marta.zielinska@techcorp.local",
                Position = "Frontend Developer",
                Department = "IT",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

            foreach (var user in users)
            {
                var existingUser = await userManager.FindByEmailAsync(
                    user.Email!);

                if (existingUser != null)
                    continue;

                var result = await userManager.CreateAsync(
                    user, "Test123!");

                if (!result.Succeeded)
                {
                    throw new Exception(
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                string role = user.FirstName switch
                {
                    "Adam" => "Administrator",
                    "Anna" => "Kierownik",
                    _ => "Pracownik"
                };

                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
