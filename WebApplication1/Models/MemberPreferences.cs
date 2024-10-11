namespace WebApplication1.Models;

public class MemberPreferences
{
    public int Id { get; set; }
    
    public List<string> FavouritePlatforms { get; set; }
    
    public List<string> FavouriteGameCategories { get; set; }

    public List<string> FavouritePreferences { get; set; }

    
}