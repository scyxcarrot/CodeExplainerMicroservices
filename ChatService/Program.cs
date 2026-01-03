using ChatService.Consumers;
using ChatService.DbContexts;
using ChatService.DelegatingHandlers;
using ChatService.HttpClients;
using ChatService.Repositories;
using CodeExplainerCommon.Constants;
using MassTransit;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Serilog;

Console.WriteLine("Starting ChatService");
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthorizationDelegatingHandler>();
builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["UserServiceUrl"]);
}).AddHttpMessageHandler<AuthorizationDelegatingHandler>();

var devPolicyName = "DevPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(devPolicyName,
        policy =>
        {
            // Allow requests from your Next.js development URL
            policy.WithOrigins(
            "https://localhost:3000",
            "https://app.code-explainer.com:3000",
            "https://app.code-explainer.com"
        )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// Add services to the container.
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

// Add services to the container.
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();

builder.Services.AddControllers();
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContextFactory<ChatDbContext>(
    options => options.UseSqlServer(connectionString),
    ServiceLifetime.Scoped);

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

builder.Services.AddMassTransit(
    busRegistrationConfigurator =>
    {
        busRegistrationConfigurator.AddEntityFrameworkOutbox<ChatDbContext>(outboxConfigurator =>
        {
            outboxConfigurator.UseSqlServer();
            outboxConfigurator.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
        });

        busRegistrationConfigurator.SetKebabCaseEndpointNameFormatter();
        busRegistrationConfigurator.AddConsumer<ChatCreateConsumer>();
        busRegistrationConfigurator.AddConsumer<UserCreateConsumer>();
        busRegistrationConfigurator.AddConsumer<UserDeleteConsumer>();

        busRegistrationConfigurator.UsingRabbitMq((context, cfg) =>
        {
            var rabbitMQHost = builder.Configuration["RabbitMQHost"];
            var rabbitMQPort = ushort.Parse(builder.Configuration["RabbitMQPort"]);
            cfg.Host(
                rabbitMQHost,
                rabbitMQPort,
                "/",
                h =>
                {
                    h.Username(builder.Configuration["RabbitMQUsername"]);
                    h.Password(builder.Configuration["RabbitMQPassword"]);
                });
            cfg.ConfigureEndpoints(context);
        });
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSerilogRequestLogging();
app.UseRouting();
app.UseCors(devPolicyName);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<ChatDbContext>>();

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


Console.WriteLine("ChatService successfully configured");
Console.WriteLine($"ChatService Url = {builder.Configuration["ChatServiceUrl"]}");
app.Run();
