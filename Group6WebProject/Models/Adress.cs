using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Group6WebProject.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Group6WebProject.Models
{
    public class Address
    {
        [Key]
        public int Id { get; set; }

        // Foreign key to User
        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [ValidateNever]
        public User? User { get; set; }

        // Address fields
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; }

        [Display(Name = "Apartment/Suite")]
        public string? ApartmentSuite { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        [Display(Name = "Province/State")]
        public string Province { get; set; }

        [Required]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }

        [Required]
        public string Country { get; set; }

        [Display(Name = "Delivery Instructions")]
        public string? DeliveryInstructions { get; set; }

        [Display(Name = "Shipping Address Same as Mailing Address")]
        public bool IsShippingSameAsMailing { get; set; } = true;

        // Shipping address fields
        [Display(Name = "Shipping Street Address")]
        public string? ShippingStreetAddress { get; set; }

        [Display(Name = "Shipping Apartment/Suite")]
        public string? ShippingApartmentSuite { get; set; }

        [Display(Name = "Shipping City")]
        public string? ShippingCity { get; set; }

        [Display(Name = "Shipping Province/State")]
        public string? ShippingProvince { get; set; }

        [Display(Name = "Shipping Postal Code")]
        public string? ShippingPostalCode { get; set; }

        [Display(Name = "Shipping Country")]
        public string? ShippingCountry { get; set; }
    }
}
