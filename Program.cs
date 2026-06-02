using LandPortal.Api.Data;
using LandPortal.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.DependencyInjection;
using LandPortal.Api;
using LandPortal.Api.Controllers;
using Razorpay.Api;
using LandPortal.Api.Entities;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
//builder.Configuration.AddEnvironmentVariables();
var configuration = builder.Configuration;
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

// -----------------------------
// Controllers + JSON options
// -----------------------------
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        opts.JsonSerializerOptions.MaxDepth = 64;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "LandPortal.Api", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// -----------------------------
// DbContext
// -----------------------------
var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in configuration.");

//builder.Services.AddDbContext<LandPortalDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddDbContext<LandPortalDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o => o.CommandTimeout(30)));

builder.Services.AddDbContext<LandPortalDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.CommandTimeout(30)).UseSnakeCaseNamingConvention());



// -----------------------------
// JWT Authentication
// -----------------------------
var jwtKey = configuration["Jwt:Key"];
var jwtIssuer = configuration["Jwt:Issuer"];
var jwtAudience = configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) || string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Missing Jwt configuration (Jwt:Key, Jwt:Issuer, Jwt:Audience).");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.Configure<PaymentWebhookOptions>(builder.Configuration.GetSection("PaymentWebhook"));
builder.Services.Configure<RazorpayOptions>(builder.Configuration.GetSection("Razorpay"));
builder.Services.AddSingleton<RazorpayClientFactory>();
builder.Services.AddScoped<IUnlockLogService, UnlockLogService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<SmsService>();
builder.Services.AddHttpClient<WhatsAppService>();
builder.Services.AddSingleton<GcpStorageService>();





// -----------------------------
// Google Storage: register StorageClient
// -----------------------------
builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();

    var saPath = cfg["Gcs:ServiceAccountJsonPath"]
                 ?? cfg["Gcs:CredentialPath"]
                 ?? cfg["GoogleCloud:CredentialsPath"];

    if (!string.IsNullOrWhiteSpace(saPath))
    {
        if (!File.Exists(saPath))
            throw new InvalidOperationException($"GCS service account file not found: {saPath}");

        var cred = GoogleCredential.FromFile(saPath);
        return StorageClient.Create(cred);
    }

    // fallback to ADC
    return StorageClient.Create();
});

// -----------------------------
// Register GoogleStorageService (try ctor(bucket, StorageClient) then fallback to bucket-only)
// -----------------------------
builder.Services.AddSingleton<GoogleStorageService>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var bucket = cfg["Gcs:Bucket"] ?? cfg["GoogleCloud:BucketName"] ?? "landportal-images";
    var storageClient = sp.GetRequiredService<StorageClient>();

    // Try to construct GoogleStorageService(bucket, storageClient) and if that fails, try GoogleStorageService(bucket)
    try
    {
        // attempt to call ctor (string bucket, StorageClient client)
        var instance = Activator.CreateInstance(typeof(GoogleStorageService), bucket, storageClient);
        if (instance is GoogleStorageService gs)
            return gs;
    }
    catch
    {
        // ignore and fallback
    }

    // fallback to (string bucket) constructor
    try
    {
        var instance = Activator.CreateInstance(typeof(GoogleStorageService), bucket);
        if (instance is GoogleStorageService gs)
            return gs;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Failed to create GoogleStorageService; please adjust the registration to match its constructor.", ex);
    }

    // unreachable
    throw new InvalidOperationException("Unable to construct GoogleStorageService.");
});

// -----------------------------
// Register GcsSignerService (depends on StorageClient, IConfiguration, ILogger)
// -----------------------------
builder.Services.AddSingleton<GcsSignerService>();

// -----------------------------
// CORS
// -----------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://landportal-frontend-c0hgf5b6bfefb9eq.centralindia-01.azurewebsites.net",
                "https://absquare.site",
                "https://www.absquare.site",
                "https://ab-2-dusky.vercel.app"  // your Vercel project
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// -----------------------------
// Build & middleware
// -----------------------------
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LandPortal.Api v1"));
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", env = app.Environment.EnvironmentName }));

app.Run();