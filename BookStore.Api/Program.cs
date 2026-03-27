using BookStore.Api.Extensions;
using BookStore.Infrastructure;
using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors.Select(error => error.ErrorMessage).ToArray());

        var validationProblemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation error",
            Detail = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };

        return new BadRequestObjectResult(validationProblemDetails);
    };
});
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseGlobalExceptionHandling();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsDevelopment() && dbContext.Database.HasPendingModelChanges())
    {
        throw new InvalidOperationException(
            "Foram detectadas alteracoes nas entidades sem migration correspondente. " +
            "Execute: dotnet dotnet-ef migrations add <NomeDaMigration>");
    }

    dbContext.Database.Migrate();
    DbSeeder.Seed(dbContext);
}

app.Run();
