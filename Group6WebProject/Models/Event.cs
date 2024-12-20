﻿namespace Group6WebProject.Models;

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime EventDate { get; set; }
    
    public ICollection<EventRegister> EventRegister { get; set; } = new List<EventRegister>();

}