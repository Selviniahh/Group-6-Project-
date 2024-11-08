using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Group6WebProject.Data;

namespace Group6WebProject.Models
{
    public class Cart
    {
        [Key]
        public int CartID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; }

        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}