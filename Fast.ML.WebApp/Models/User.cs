using Microsoft.AspNetCore.Identity;

namespace Fast.ML.WebApp.Models;

public class User : IdentityUser
{
    [ProtectedPersonalData]
    public new int Id { get; set; }
    
    [PersonalData]
    public new string Email { get; set; }
    
    [PersonalData]
    public string FirstName { get; set; }
    
    [PersonalData]
    public string LastName { get; set; }
}