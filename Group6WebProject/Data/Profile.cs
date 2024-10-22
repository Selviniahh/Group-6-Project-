using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Group6WebProject.Data;

public enum Gender
{
    NotSelected,
    Male,
    Female,
    PreferNotToSay,
}

public class Profile
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [ReadOnly(true)]
    [Display(Name = "Name")]
    public string Name { get; set; }    
    
    [ReadOnly(true)]
    [Display(Name = "Email")]
    public string? Email { get; set; }
    
    // ... Other properties ...
}