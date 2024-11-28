using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Group6WebProject.Data;

public enum Gender
{
    Male,
    Female,
    PreferNotToSay,
}

// Let members enter their actual name, gender, and birth date
public class Profile
{
    [Key]
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Display(Name = "Name")]
    [MaxLength(250)]
    public string? Name { get; set; }    
    
    [Display (Name = "Gender")]
    public Gender? Gender { get; set; }
    
    [Display(Name = "Birth Date")]
    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    
    [Display(Name = "Receive CVGS")]
    public bool ReceiveCvgs { get; set; }
    
    // ... Other properties ...
}