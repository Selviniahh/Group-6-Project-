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
    
    [Display(Name = "Profile Picture")]
    public string? ProfilePicture { get; set; }
    
    [Display(Name = "Favourite Video Game")]
    public string? FavouriteVideoGame { get; set; }
    
    [Display(Name = "Biography")]
    public string? Biography { get; set; }
    
    [Display(Name = "Date of Birth")]
    public DateTime DateOfBirth { get; set; }
    
    [Display(Name = "Gender")]
    public Gender Gender { get; set; }
    
    [Display(Name = "Country")]
    public string Country { get; set; }
    
    [ReadOnly(true)]
    [Display(Name = "Last Login")]
    public DateTime LastLogin { get; set; }
    
    [Display(Name = "Contact Number")]
    public string? ContactNumber { get; set; }
    
    [Display(Name = "Receive Promotional Emails")]
    public bool ReceivePromotionalEmails { get; set; }
    
    
    
}