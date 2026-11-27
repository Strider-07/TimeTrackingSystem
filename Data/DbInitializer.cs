using Microsoft.AspNetCore.Identity;

namespace TimeTrackingSystem.Data
{
    static class DbInitializer
    {
        public static async Task InitializeRolesAsync(RoleManager<IdentityRole> _roleManager)
        {
            string[] names = ["Administrator", "Kierownik", "Pracownik"];

            foreach (var role in names)
            {   
                bool result = await _roleManager.RoleExistsAsync(role);
                if(!result)
                {
                    var newRole = new IdentityRole(role);
                    var roleResult = await _roleManager.CreateAsync(newRole);
                    if(!roleResult.Succeeded)
                    {
                         var errors = string.Join(", ", roleResult.Errors.Select(error => error.Description));
                         throw new InvalidOperationException(errors);   
                        
                    }
                }
            }

        }
    }
}
