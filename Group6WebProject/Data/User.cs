using System.ComponentModel.DataAnnotations;

namespace Group6WebProject.Data
{
    public enum EnrollmentStatus
    {
        ConfirmationMessageNotSent,
        ConfirmationMessageSent,
        EnrollmentConfirmed,
        EnrollmentDeclined
    }
    
    public class User
    {
        [Key]
        public int UserID { get; set; }
        
        [Required(ErrorMessage = "Student name is required.")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }
        
        [Required]
        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.ConfirmationMessageNotSent;


        [Required(ErrorMessage = "Password is required.")]
        public string PasswordHash { get; set; }
        
    }    
}

