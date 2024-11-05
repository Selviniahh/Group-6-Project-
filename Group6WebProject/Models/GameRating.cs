using Group6WebProject.Data;

namespace Group6WebProject.Models
{
    public class GameRating
    {
        public int GameRatingID { get; set; }
        public int UserID { get; set; }  // Foreign Key to User
        public int GameID { get; set; }  // Foreign Key to Game
        public int Rating { get; set; }  // Rating (1-5 scale)
        public DateTime DateRated { get; set; } = DateTime.Now;

        // Navigation properties
        public User User { get; set; }  // Navigation to User
        public Game Game { get; set; }  // Navigation to Game
    }
}