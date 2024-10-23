using System.ComponentModel.DataAnnotations;

namespace Group6WebProject.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        //Example: 12345678!
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "The password must be at least 8 characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [RegularExpression(@"^(?=.*[0-9])(?=.*[\W_]).+$", ErrorMessage = "The password must contain at least one number and one special character.")]
        public string Password { get; set; } 

        [Required(ErrorMessage = "Password confirmation is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation do not match.")]
        public string ConfirmPassword { get; set; }

        // Add this property
        [Required(ErrorMessage = "Please complete the reCAPTCHA validation.")]
        [Display(Name = "reCAPTCHA")]
        public string ReCaptchaToken { get; set; }
    }
}