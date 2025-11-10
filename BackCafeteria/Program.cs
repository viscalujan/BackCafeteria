using BackCafeteria.Models;
using BackCafeteria.Services;
using CafeteriaAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var corsPolicyName = "CafeteriaCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicyName, policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ================== SWAGGER ==================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BackCafeteria API",
        Version = "v1",
        Description = "API protegida con JWT para la aplicación de cafetería"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduce el token JWT con el prefijo 'Bearer'. Ejemplo: **Bearer {tu_token}**"
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
                }
            },
            Array.Empty<string>()
        }
    });
});

// ================== JWT ==================
var secretKey = builder.Configuration["settings:secretkey"];
var keyBytes = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(config =>
{
    config.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    config.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(config =>
{
    config.RequireHttpsMetadata = false;
    config.SaveToken = true;
    config.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// ================== DB Context ==================
builder.Services.AddDbContext<CafeteriaDbv2Context>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("CafeteriaDBv2"),
        new MySqlServerVersion(new Version(8, 4, 6))));

// ================== SESIÓN ==================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<EmailService>();

var app = builder.Build();

// ================== MIDDLEWARE ==================
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors(corsPolicyName);

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
