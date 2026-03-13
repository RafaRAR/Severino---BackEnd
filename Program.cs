using APIseverino.Data;
using APIseverino.Helpers;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

if (File.Exists(".env.test"))
    Env.Load(".env.test");
else if (File.Exists(".env"))
    Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<ImageKitService>();

// DB (Postgres)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("DATABASE_URL")));

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