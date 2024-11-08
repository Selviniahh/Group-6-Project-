using System.ComponentModel.DataAnnotations;

namespace Group6WebProject.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemID { get; set; }

        public int OrderID { get; set; }
        public Order Order { get; set; }

        public int GameID { get; set; }
        public Game Game { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }
    }
}