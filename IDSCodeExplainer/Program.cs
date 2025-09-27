using System.ClientModel;

using IDSCodeExplainer.DelegatingHandlers;
using IDSCodeExplainer.HttpClients;
using IDSCodeExplainer.Services;
using IDSCodeExplainer.Services.Ingestion;

using MassTransit;

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
// Load the production configuration first
builder.Configuration.AddJsonFile(
    Path.Combine(parentPath.FullName, "common_appsettings.json"),
    optional: true,
    reloadOnChange: true);

// load the development configuration to override it
builder.Configuration.AddJsonFile(
    Path.Combine(parentPath.FullName, "common_appsettings.Development.json"),
    optional: true,
    reloadOnChange: true);

// Then, load the local appsettings.json.
// This allows local settings to override shared ones.
builder.Configuration.AddJsonFile(
    "appsettings.json",
    optional: false,
    reloadOnChange: true);

// Add http clients
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthorizationDelegatingHandler>();
builder.Services.AddHttpClient<IChatServiceClient, ChatServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ChatServiceUrl"]);
}).AddHttpMessageHandler<AuthorizationDelegatingHandler>();

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

var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "VectorStore.db");
var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
builder.Services.AddSqliteCollection<string, IngestedChunk>("data-IDSCodeExplainer-chunks", vectorStoreConnectionString);
builder.Services.AddSqliteCollection<string, IngestedDocument>("data-IDSCodeExplainer-documents", vectorStoreConnectionString);

builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();

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

builder.Services.AddMassTransit(
    busRegistrationConfigurator =>
    {
        busRegistrationConfigurator.SetKebabCaseEndpointNameFormatter();
        // Register all
        //busRegistrationConfigurator.AddConsumers(typeof(Program).Assembly);

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
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// By default, we ingest PDF files from the /wwwroot/Data directory. You can ingest from
// other sources by implementing IIngestionSource.
// Important: ensure that any content you ingest is trusted, as it may be reflected back
// to users or could be a source of prompt injection risk.
//await DataIngestor.IngestDataAsync(
//    app.Services,
//    new CodeFileDirectorySource(Path.Combine(contentRootPath, "Data")));

app.Run();
