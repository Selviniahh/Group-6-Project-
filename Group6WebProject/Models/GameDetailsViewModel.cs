using System.Collections.Generic;

namespace Group6WebProject.Models
{
    public class GameDetailsViewModel
    {
        public Game Game { get; set; }
        public double AverageRating { get; set; }
        public List<Game> GameRecommendations { get; set; }
        public bool HasPurchased { get; set; }
        public bool IsFree { get; set; }
    }
}