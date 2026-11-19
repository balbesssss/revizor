using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Revizor_Service.Database;
using Revizor_Service.Database.Models;
using Revizor_Service.DTO;
using Revizor_Service.Services;

namespace Revizor_Service.Extension;

public static class ServicesBuilder
{
    public static void RegisterServices (this WebApplicationBuilder builder)
    {

        builder.Services.AddOpenApi();
        builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("revizor")));
        builder.Services.AddSwaggerGen(opt =>
        {
            opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name="Authoriation",
                Description="JWT",
                In=ParameterLocation.Header,
                Type=SecuritySchemeType.Http,
                Scheme=JwtBearerDefaults.AuthenticationScheme,
                BearerFormat="JWT"
            });
            opt.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer",doc)] =[] 
            });
        });
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateLifetime=true,
                ValidateIssuer=true,
                ValidateIssuerSigningKey=true,
                ValidIssuer=builder.Configuration["JWT:Issuer"],
                ValidAudience=builder.Configuration["JWT:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!)), 
                ClockSkew = TimeSpan.Zero
            };
        });
        builder.Services.AddAuthorization(opt =>
        {
            opt.AddPolicy("admin",p => p.RequireRole("admin"));
            opt.AddPolicy("revizor", p=> p.RequireRole("revizor"));
        });
        builder.Services.AddScoped<IPasswordHasher<CreateUser>, PasswordHasher<CreateUser>>();
        builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        builder.Services.AddScoped<ReportsServices>();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithExposedHeaders("Content-Disposition");
            });
        });
        builder.Services.AddValidation();
        builder.Services.AddMemoryCache();
    }
}
