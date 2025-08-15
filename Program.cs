using UserManagement.Models;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add Swagger support (services)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ✅ 1. Developer tools & exception pages
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ 2. Global error handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var errorResponse = new { message = "An unexpected error occurred.", detail = ex.Message };
        await context.Response.WriteAsJsonAsync(errorResponse);
    }
});

// ✅ 3. Logging middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"➡️ {context.Request.Method} {context.Request.Path}");
    await next(context);
    Console.WriteLine($"⬅️ {context.Response.StatusCode}");
});


var users = new List<User>
{
    new User { Id = 1, Name = "Alice", Email = "alice@example.com" },
    new User { Id = 2, Name = "Bob", Email = "bob@example.com" }
};

// Get all users
app.MapGet("/users", () => users);

// Get user by id
app.MapGet("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

// Create new user
app.MapPost("/users", (User user) =>
{
    var validation = ValidateUser(user);
    if (!validation.IsValid)
        return Results.BadRequest(new { message = validation.Error });

    user.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
    users.Add(user);
    return Results.Created($"/users/{user.Id}", user);
});

// Update user
app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);

    if (user is null) return Results.NotFound();

    var validation = ValidateUser(updatedUser);
    if (!validation.IsValid)
        return Results.BadRequest(new { message = validation.Error });
        

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;

    return Results.Ok(user);
});

// Delete user
app.MapDelete("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();

    users.Remove(user);
    return Results.NoContent();
});

// Validation Method
static (bool IsValid, string Error) ValidateUser(User user)
{
    if (string.IsNullOrEmpty(user.Name))
    {
        return (false, "Name cannot be empty.");
    }

    if (string.IsNullOrEmpty(user.Email))
    {
        return (false, "Email cannot be empty.");
    }

    // Basic email regex
    var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    if (!Regex.IsMatch(user.Email, emailPattern))
    {
        return (false, "Invalid email format.");
    }

    return (true, string.Empty);
}

app.Run();
