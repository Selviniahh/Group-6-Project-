using Group6WebProject.Data;

namespace Group6WebProject.Models
{
    public class GameReview
    {
        public int GameReviewID { get; set; }
        public int UserID { get; set; }  // Foreign Key to User
        public int GameID { get; set; }  // Foreign Key to Game
        public string ReviewText { get; set; }
        public string ReviewStatus { get; set; } = "Pending"; // Default to "Pending"
        public DateTime SubmissionDate { get; set; } = DateTime.Now;
        public DateTime? ApprovalDate { get; set; }

        // Navigation properties
        public User User { get; set; }
        public Game Game { get; set; }
    }
}