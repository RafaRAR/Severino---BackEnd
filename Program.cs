using APIseverino.Data;
using APIseverino.Helpers;
using APIseverino.Hubs;
using APIseverino.Models;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<ImageKitService>();

// ─── Carrega .env (tenta ambos os nomes) ─────────────────────────────────────
var envPath1 = Path.Combine(Directory.GetCurrentDirectory(), ".env.test");
var envPath2 = Path.Combine(Directory.GetCurrentDirectory(), "env.test");
if (File.Exists(envPath1))
    Env.Load(envPath1);
else if (File.Exists(envPath2))
    Env.Load(envPath2);

// ─── Banco de Dados (DATABASE_URL ou appsettings) ─────────────────────────────
var rawDb = Environment.GetEnvironmentVariable("DATABASE_URL");
Console.WriteLine("DATABASE_URL presente: " + rawDb);

string connStr = null;
if (!string.IsNullOrEmpty(rawDb))
{
    if (rawDb.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(rawDb);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builderConn = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port,
            Username = userInfo.Length > 0 ? userInfo[0] : "",
            Password = userInfo.Length > 1 ? userInfo[1] : "",
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Require
        };
        connStr = builderConn.ToString();
    }
    else
    {
        connStr = rawDb;
        try
        {
            _ = new NpgsqlConnectionStringBuilder(rawDb);
            connStr = rawDb;
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Warning: DATABASE_URL inválida: {ex.Message}. Usando DefaultConnection.");
            connStr = null;
        }
    }
}

// Fallback para appsettings
if (string.IsNullOrEmpty(connStr))
    connStr = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connStr))
    throw new InvalidOperationException("Connection string não configurada. Defina DATABASE_URL ou DefaultConnection.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connStr));

// ─── Controllers + Swagger ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ─── Email ────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IEmailService, EmailService>();

// ─── JWT ──────────────────────────────────────────────────────────────────────
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"] ?? "ChaveSegurancaPadraoMuitoLongaParaEvitarErros");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        // Permite que o SignalR envie o token via query string (?access_token=...)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ─── CORS ─────────────────────────────────────────────────────────────────────
// AllowAnyOrigin() é incompatível com AllowCredentials() (exigido pelo SignalR).
// Enquanto não tiver a URL definitiva do frontend, use SetIsOriginAllowed para
// liberar tudo sem quebrar o WebSocket. Quando tiver a URL, troque por:
//   policy.WithOrigins("https://sua-url.azurestaticapps.net")
builder.Services.AddCors(options =>
{
    options.AddPolicy("AzurePolicy", policy =>
    {
        policy
          //.policy.WithOrigins("https://white-smoke-05cde4b0f.4.azurestaticapps.net")
            .SetIsOriginAllowed(_ => true) // equivalente a AllowAnyOrigin, mas compatível com AllowCredentials
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // obrigatório para SignalR WebSocket
    });
});

// ─── SignalR ──────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ─── Background Service (expira posts a cada 1h) ──────────────────────────────
builder.Services.AddHostedService<PostExpiracaoService>();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// CORS deve vir antes de Auth e do MapHub
app.UseCors("AzurePolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Hub do SignalR — frontend conecta em: ws://seudominio/chathub?access_token=SEU_JWT
app.MapHub<ChatHub>("/chathub");

app.Run();