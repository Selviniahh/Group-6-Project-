using Group6WebProject.Data;

namespace Group6WebProject.Models;

public class EventRegister
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }

    public User User { get; set; }
    public Event Event { get; set; }
}