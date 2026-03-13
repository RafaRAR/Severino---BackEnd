using APIseverino.Data;
using APIseverino.Helpers;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<ImageKitService>();

// Carrega .env (tenta ambos os nomes)
var envPath1 = Path.Combine(Directory.GetCurrentDirectory(), ".env.test");
var envPath2 = Path.Combine(Directory.GetCurrentDirectory(), "env.test");
if (File.Exists(envPath1))
    Env.Load(envPath1);
else if (File.Exists(envPath2))
    Env.Load(envPath2);

// Lê variável e converte se necessário
var rawDb = Environment.GetEnvironmentVariable("DATABASE_URL");
Console.WriteLine("DATABASE_URL presente: " + (!string.IsNullOrEmpty(rawDb)));

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
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };
        connStr = builderConn.ToString();
    }
    else
    {
        // Assume já é uma connection string Npgsql válida
        connStr = rawDb;
    }
}

// Fallback para appsettings
if (string.IsNullOrEmpty(connStr))
    connStr = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connStr))
    throw new InvalidOperationException("Connection string não configurada. Defina DATABASE_URL ou DefaultConnection.");

// Registrar DbContext com connection string válida
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connStr));

// ... restante da configuração (controllers, swagger, auth, etc.)

// Controllers
builder.Services.AddControllers();

// 🔥 Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar serviço de email (implementação usa MailKit / Resend conforme configurado)
builder.Services.AddScoped<IEmailService, EmailService>();

// JWT
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);

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
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options => {
    options.AddPolicy("AzurePolicy", policy => {
        //policy.WithOrigins("https://white-smoke-05cde4b0f.4.azurestaticapps.net")
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AzurePolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();