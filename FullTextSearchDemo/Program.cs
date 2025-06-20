using Blogs.Api.Database;
using Blogs.Api.Extensions;
using Blogs.Api.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<BlogsDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("blogs")));

builder.Services.AddHostedService<SeedDatabase>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.ApplyMigrations();

app.Run();