using Group6WebProject.Controllers;
using Group6WebProject.Models;
using Group6WebProject.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Group6WebProject.Tests;

public class EventControllerTests
{
    //Return all the events in the database
    [Fact]
    public void AllEvents_ReturnsAllEvents()
    {
        // Gather Events
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestCaseDB1")
            .Options;

        using var context = new ApplicationDbContext(options);
        context.Events.AddRange(
            new Event { Id = 1, Name = "Event A", Description = "Test Event"},
            new Event { Id = 2, Name = "Event B", Description = "Test Event"}
        );
        context.SaveChanges();

        var controller = new EventsController(context);

        // Act
        var result = controller.AllEvents() as ViewResult;
        var model = result?.Model as List<Event>;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, model?.Count);
        Assert.Contains(model, e => e.Name == "Event A");
        Assert.Contains(model, e => e.Name == "Event B");
    }
    
    //Return event not found if the id given is an invalid ID
    [Fact]
    public void EventDetails_InvalidEventId()
    {
        //  Gather Events
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestCaseDB2")
            .Options;

        using var context = new ApplicationDbContext(options);
        context.Events.Add(new Event { Id = 1, Name = "Event A", Description = "Test invalid event id"});
        context.SaveChanges();

        var controller = new EventsController(context);

        // Act
        var result = controller.EventDetails(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
    
    //Registering for an event 
    [Fact]
    public void RegisterForEvent_TestCase()
    {
        // Gather events
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestCaseDB3")
            .Options;

        using var context = new ApplicationDbContext(options);
        var userId = 1;
        var eventId = 1;
        context.Users.Add(new User { UserID = userId, Name = "Test User", Email = "testcase@email.com", PasswordHash = "password"});
        context.Events.Add(new Event { Id = eventId, Name = "Sample Event", Description = "Test event"});
        context.SaveChanges();

        var controller = new EventsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    }, "mock"))
                }
            },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };

        // Act
        var result = controller.RegisterForEvent(eventId) as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EventDetails", result.ViewName);
        var registration = context.EventRegister.FirstOrDefault(r => r.UserId == userId && r.EventId == eventId);
        Assert.NotNull(registration);
        Assert.Equal("You have registered for this event, see you there!", controller.TempData["SuccessMessage"]);
    }

}