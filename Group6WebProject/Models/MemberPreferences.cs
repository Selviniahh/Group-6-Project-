using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Group6WebProject.Data;

namespace Group6WebProject.Models;

public class MemberPreferences
{
    

    [Key]
    public int Id { get; set; }
    
    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public List<string> FavouritePlatforms { get; set; } = new List<string>();
    public List<string> FavouriteGameCategories { get; set; }= new List<string>();
    public List<string> LanguagePreferences { get; set; } = new List<string>();
}