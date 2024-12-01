using Group6WebProject.Data;

namespace Group6WebProject.Models;

public class MemberDetailViewModel
{
    public User User { get; set; }
    public List<Event> RegisteredEvents { get; set; }
}
