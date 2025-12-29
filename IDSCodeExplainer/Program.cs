using System.ClientModel;

using CodeExplainerCommon.Constants;

using IDSCodeExplainer.DelegatingHandlers;
using IDSCodeExplainer.HttpClients;
using IDSCodeExplainer.Services.Ingestion;

using MassTransit;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.AI;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel.Connectors.Qdrant;

using OllamaSharp;

using OpenAI;
using OpenAI.Chat;

using Qdrant.Client;

using Serilog;

Console.WriteLine("Starting IDSCodeExplainer");

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
builder.Services.AddHttpClient<IChatServiceClient, ChatServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ChatServiceUrl"]);
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
            "https://app.code-explainer.com:3000"
        )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// model for chat service, switch as needed
ApiKeyCredential credential;
if (builder.Environment.IsDevelopment())
{
    credential = new ApiKeyCredential(builder.Configuration["GitHubModelToken"] ?? 
        throw new InvalidOperationException("Missing configuration: GitHubModelToken. Configure it at secrets.json"));
}
else
{
    credential = new ApiKeyCredential(Environment.GetEnvironmentVariable("GitHubModelToken") ?? 
        throw new InvalidOperationException("Missing configuration: GitHubModelToken. Configure it at docker run -e \"GitHubModelToken=your_token_here\" justinwcy/code_explainer_ids_code_explainer"));
}

var openAIClientOptions = new OpenAIClientOptions()
{
    Endpoint = new Uri("https://models.github.ai/inference")
};

// microsoft/MAI-DS-R1 is for debugging purposes only. Expensive!

// models that support tools
// ai21-labs/AI21-Jamba-1.5-Mini
// microsoft/Phi-4-mini-instruct
// mistral-ai/Ministral-3B
// mistral-ai/Mistral-Nemo
var chatClient =
    new ChatClient("mistral-ai/Ministral-3B", credential, openAIClientOptions)
        .AsIChatClient();

var ollamaEndpoint = new Uri(builder.Configuration["OllamaEndpoint"]!);
//IChatClient chatClient = new OllamaApiClient(ollamaEndpoint, "granite4:1b");
builder.Services.AddChatClient(chatClient).UseFunctionInvocation().UseLogging();

// model for training
var embeddingGenerator = new OllamaApiClient(
    ollamaEndpoint,
    "embeddinggemma:latest");
builder.Services.AddEmbeddingGenerator(embeddingGenerator);

// register qdrant
string qdrantHost = builder.Configuration["QdrantHost"]!;
const int qdrantGrpcPort = 6334;

// Register Qdrant gRPC client
builder.Services.AddSingleton(_ =>
    new QdrantClient(
        host: qdrantHost,
        port: qdrantGrpcPort
    )
);

// Register Vector Store
builder.Services.AddQdrantVectorStore(
    host: qdrantHost,
    port: qdrantGrpcPort,
    https: false,
    apiKey: null,
    options: new QdrantVectorStoreOptions
    {
        EmbeddingGenerator = embeddingGenerator
    }
);

string codeChunkCollectionName = "CodeExplainer-IDS-CodeChunk";
string codeDocumentCollectionName = "CodeExplainer-IDS-CodeDocument";
builder.Services.AddQdrantCollection<Guid, CodeChunk>(codeChunkCollectionName);
builder.Services.AddQdrantCollection<Guid, CodeDocument>(codeDocumentCollectionName);

builder.Services.AddSingleton<SemanticSearch>();

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
app.UseCors(devPolicyName);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// By default, we ingest PDF files from the /wwwroot/Data directory. You can ingest from
// other sources by implementing IIngestionSource.
// Important: ensure that any content you ingest is trusted, as it may be reflected back
// to users or could be a source of prompt injection risk.
//await DataIngestor.IngestDataAsync(
//    app.Services,
//    new CodeFileDirectorySource(Path.Combine(contentRootPath, "Data")));
Console.WriteLine("IDSCodeExplainer successfully configured");
Console.WriteLine($"ChatService Url = {builder.Configuration["ChatServiceUrl"]}");

Console.WriteLine($"QDrant host = {qdrantHost}");
Console.WriteLine($"QDrant port = {qdrantGrpcPort}");

var qdrantClient = app.Services.GetRequiredService<QdrantClient>();
var codeChunkCollectionInfo = await qdrantClient.GetCollectionInfoAsync(codeChunkCollectionName);
var codeChunkStatus = codeChunkCollectionInfo.Status;
Console.WriteLine($"CodeChunk Collection launched with status = {codeChunkStatus}");

var codeDocumentCollectionInfo = await qdrantClient.GetCollectionInfoAsync(codeDocumentCollectionName);
var codeDocumentStatus = codeDocumentCollectionInfo.Status;
Console.WriteLine($"CodeDocument Collection launched with status = {codeDocumentStatus}");
app.Run();
