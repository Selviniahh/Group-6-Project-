using Group6WebProject.Data;

namespace Group6WebProject.Models;

public class WishlistItem
{
    public int WishlistItemId { get; set; }
    
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public int? GameId { get; set; }
    public Game? Game { get; set; }

    public bool IsPublic { get; set; } = true;
 

}