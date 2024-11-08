using System.ComponentModel.DataAnnotations;

namespace Group6WebProject.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemID { get; set; }

        public int CartID { get; set; }
        public Cart Cart { get; set; }

        public int GameID { get; set; }
        public Game Game { get; set; }

        public int Quantity { get; set; } = 1;
    }
}