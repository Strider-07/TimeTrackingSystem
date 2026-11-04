using Microsoft.AspNetCore.Identity;

namespace TimeTrackingSystem.Data
{
    static class DbInitializer
    {
        //private static readonly RoleManager<IdentityRole> _roleManager; //TODO: get set?
        
        public static async Task InitializeRolesAsync(RoleManager<IdentityRole> _roleManager)
        {
            //bool existsAdmin = await _roleManager.RoleExistsAsync("Administrator");
            //bool existsKierownik = await _roleManager.RoleExistsAsync("Kierownik");
            //bool existsPracownik = await _roleManager.RoleExistsAsync("Pracownik");

            string[] nazwy = ["Administrator", "Kierownik", "Pracownik"];

            foreach (var role in nazwy)
            {   
                bool wynik = await _roleManager.RoleExistsAsync(role);
                if(!wynik) //TODO : Change the argument back to the full thing instead of a variable an check if the result.succeded is still null
                {
                    var newRole = new IdentityRole(role);
                    var result = await _roleManager.CreateAsync(newRole);
                    if(!result.Succeeded)
                    {
                          
                         var errors = string.Join(", ", result.Errors.Select(error => error.Description));
                         throw new InvalidOperationException(errors);   
                        
                    }
                }
            }

        }
    }
}
