using Happymeal.API.Configuration;
using Happymeal.Application.Configuration;
using Happymeal.Infrastructure.Configuration;
using Happymeal.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<HappymealDbContext>();
    await context.Database.MigrateAsync();
}

app.UseApiServices();

app.Run();
