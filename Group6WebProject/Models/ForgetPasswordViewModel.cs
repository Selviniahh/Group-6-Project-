using System.ComponentModel.DataAnnotations;

namespace Group6WebProject.Models
{
    public class ForgetPasswordViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}