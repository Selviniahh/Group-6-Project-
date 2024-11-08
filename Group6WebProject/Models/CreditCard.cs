using System.ComponentModel.DataAnnotations;
using Group6WebProject.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Group6WebProject.Models
{
    public class CreditCard
    {
        [Key]
        public int CreditCardID { get; set; }

        public int UserID { get; set; }
        
        [ValidateNever]
        public User User { get; set; }

        [Required]
        [Display(Name = "Card Number")]
        [CreditCard(ErrorMessage = "Invalid credit card number.")]
        public string CardNumber { get; set; }

        [Required]
        [Display(Name = "Cardholder Name")]
        public string CardHolderName { get; set; }

        [Required]
        [Display(Name = "Expiration Month")]
        [Range(1, 12, ErrorMessage = "Expiration month must be between 1 and 12.")]
        public int ExpirationMonth { get; set; }

        [Required]
        [Display(Name = "Expiration Year")]
        [Range(2023, 2100, ErrorMessage = "Invalid expiration year.")]
        public int ExpirationYear { get; set; }

        [Required]
        [Display(Name = "CVV")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "Invalid CVV.")]
        public string CVV { get; set; }
    }
}