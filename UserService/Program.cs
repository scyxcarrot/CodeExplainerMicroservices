using System;
using CodeExplainerCommon.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Serilog;
using UserService.DbContexts;
using UserService.DelegatingHandlers;
using UserService.HttpClients;
using UserService.Models;
using UserService.Repositories;
using UserService.Service;

Console.WriteLine("Starting UserService");
var builder = WebApplication.CreateBuilder(args);
var contentRootPath = builder.Environment.ContentRootPath;
var parentPath = Directory.GetParent(contentRootPath);
// Load the production configuration first
builder.Configuration.AddJsonFile(
    Path.Combine(parentPath.FullName, "common_appsettings.json"),
    optional: true,
    reloadOnChange: true);

// Then, load the local appsettings.json.
// This allows local settings to override shared ones.
builder.Configuration.AddJsonFile(
    "appsettings.json",
    optional: false,
    reloadOnChange: true);

// load the development configuration to override it
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        Path.Combine(parentPath.FullName, "common_appsettings.Development.json"),
        optional: true,
        reloadOnChange: true);

    builder.Configuration.AddJsonFile(
        "appsettings.Development.json",
        optional: false,
        reloadOnChange: true);
}

// Add http clients
builder.Services.AddTransient<AuthorizationDelegatingHandler>();
builder.Services.AddHttpClient<IChatServiceClient, ChatServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ChatServiceUrl"]);
}).AddHttpMessageHandler<AuthorizationDelegatingHandler>();

// Add services to the container.
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddControllers();
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContextFactory<UserDbContext>(
    options => options.UseSqlServer(connectionString),
    ServiceLifetime.Scoped);
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddIdentity<AppUser, IdentityRole>(
        options =>
        {
            options.Password.RequiredLength = 6;
        })
    .AddEntityFrameworkStores<UserDbContext>();

builder.Services.AddAuthentication(options =>
    {
        // Set ALL defaults to the JWT Bearer scheme
        options.DefaultAuthenticateScheme =
            options.DefaultScheme =
                options.DefaultSignInScheme =
                    options.DefaultSignOutScheme =
                        options.DefaultChallengeScheme =
                            options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;

    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    builder.Configuration["JWT:SigningKey"])),

        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies[Token.AccessToken];
                return Task.CompletedTask;
            }
        };

    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<UserDbContext>>();
    await using var dbContext = await dbContextFactory.CreateDbContextAsync();
    Console.WriteLine("--> Attempting to run migrations");
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during migration: {ex.Message}");
    }
}

Console.WriteLine("UserService successfully configured");
Console.WriteLine($"ChatService Url = {builder.Configuration["ChatServiceUrl"]}");
app.Run();
