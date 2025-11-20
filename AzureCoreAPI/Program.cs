using Azure.Messaging.ServiceBus;
using AzureCoreAPI.DependencyInjection;
using AzureCoreAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.AzureAppServices;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Auth using Azure AD
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddMicrosoftIdentityWebApi(options =>
                        {
                            configuration.Bind("AzureAd", options);
                            options.Events = new JwtBearerEvents();

                            options.Events = new JwtBearerEvents
                            {
                                OnTokenValidated = context =>
                                {
                                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                                    // Access the scope claim (scp) directly
                                    var scopeClaim = context.Principal?.Claims.FirstOrDefault(c => c.Type == "scp")?.Value;

                                    if (scopeClaim != null)
                                    {
                                        logger.LogInformation("Scope found in token: {Scope}", scopeClaim);
                                    }
                                    else
                                    {
                                        logger.LogWarning("Scope claim not found in token.");
                                    }

                                    return Task.CompletedTask;
                                },
                                OnAuthenticationFailed = context =>
                                {
                                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                                    logger.LogError("Authentication failed: {Message}", context.Exception.Message);
                                    return Task.CompletedTask;
                                },
                                OnChallenge = context =>
                                {
                                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                                    logger.LogError("Challenge error: {ErrorDescription}", context.ErrorDescription);
                                    return Task.CompletedTask;
                                }
                            };
                        }, options => { configuration.Bind("AzureAd", options); });

// The following flag can be used to get more descriptive errors in development environments
IdentityModelEventSource.ShowPII = false;

builder.Logging.AddAzureWebAppDiagnostics(); // 👈 custom file path
builder.Services.Configure<AzureFileLoggerOptions>(options =>
{
    options.FileName = "realtimelog";
    options.FileSizeLimit = 50 * 1024;
    options.RetainedFileCountLimit = 5;
});

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowSpecificOrigin",
                      builder =>
                      {
                          builder.WithOrigins(["https://rituraj-angular-d8h7eccubkech2ae.eastasia-01.azurewebsites.net", "http://localhost:4200"]) // Replace with your Angular app's origin
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                      });
});
// Inject Service Bus
builder.Services.AddScoped(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var conn = config["AzureServiceBus:ConnectionString"];
    return new ServiceBusClient(conn);
});

builder.Services.AddScoped<IServiceBusSender, ServiceBusSenderService>();
builder.Services.AddScoped<ServiceBusConsumerService>();

// Inject cosmos DB
builder.Services.AddScoped<CosmosClient>(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    return new CosmosClient(
        config["CosmosDb:AccountEndpoint"],
        config["CosmosDb:AccountKey"]);
});

builder.Services.AddScoped<ICosmosDbService, CosmosDbService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigin");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
