using System.ClientModel;

using IDSCodeExplainer.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Tokens;

using OllamaSharp;

using OpenAI;
using OpenAI.Chat;

using Serilog;

var builder = WebApplication.CreateBuilder(args);
var contentRootPath = builder.Environment.ContentRootPath;
var parentPath = Directory.GetParent(contentRootPath);
// Load the shared configuration first
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

// model for chat service, switch as needed
var credential = new ApiKeyCredential(builder.Configuration["GitHubModels:Token"] ?? throw new InvalidOperationException("Missing configuration: GitHubModels:Token. configure it at secrets.json"));
var openAIClientOptions = new OpenAIClientOptions()
{
    Endpoint = new Uri("https://models.github.ai/inference")
};

// models that support tools
// ai21-labs/AI21-Jamba-1.5-Mini
// microsoft/Phi-4-mini-instruct
// mistral-ai/Ministral-3B
// mistral-ai/Mistral-Nemo
var chatClient = 
    new ChatClient("mistral-ai/Ministral-3B", credential, openAIClientOptions)
        .AsIChatClient();
builder.Services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();

// model for training
var embeddingGenerator = new OllamaApiClient(
    new Uri("http://localhost:11434"),
    "nomic-embed-text:latest");
builder.Services.AddEmbeddingGenerator(embeddingGenerator);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddScoped<FileService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(options =>
    {
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
    });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
