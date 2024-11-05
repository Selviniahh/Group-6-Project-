﻿namespace Group6WebProject.Models;

public class Game
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string Genre { get; set; }
    public string Price { get; set; }
    public string Platform{ get; set; }
    public ICollection<GameReview> Reviews { get; set; } 
    public ICollection<GameRating> Ratings { get; set; } 
    public double AverageRating()
        {
            return Ratings.Any() ? Ratings.Average(r => r.Rating) : 0;
        }
}