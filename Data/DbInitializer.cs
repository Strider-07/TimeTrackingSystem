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
                if(!result) //TODO : Change the argument back to the full thing instead of a variable an check if the result.succeded is still null
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
