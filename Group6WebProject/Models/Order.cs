using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Group6WebProject.Data;

namespace Group6WebProject.Models
{
    public class Order
    {
        [Key]
        public int OrderID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; }

        public DateTime OrderDate { get; set; }

        public string Status { get; set; } = "Pending";

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public int AddressID { get; set; }
        public Address ShippingAddress { get; set; }

        public int CreditCardID { get; set; }
        public CreditCard CreditCard { get; set; }
    }
}