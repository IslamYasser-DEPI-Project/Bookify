using Bookify.Application.Interfaces;
using Bookify.Application.Services.Admin_Services;
using Bookify.Application.Services.Registeration_Services;
using Bookify.DA;
using Bookify.DA.Data;
using Bookify.DA.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System;
using Microsoft.AspNetCore.Http;
using Bookify.Application.Services;

namespace Bookify.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            //session support 
            builder.Services.AddDistributedMemoryCache();
         
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.Name = ".Bookify.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                //Lax for typical web flows, consider Strict for higher security needs.
                options.Cookie.SameSite = SameSiteMode.Lax;
                //force Secure.
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            // Configure Swagger with JWT Bearer support
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bookify API", Version = "v1" });

                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter '{token}'"
                };

                c.AddSecurityDefinition("Bearer", securityScheme);

                // Apply to all operations
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
                c.OperationFilter<AuthorizeCheckOperationFilter>();
            });


            builder.Services.AddCors(o =>
            {
                o.AddPolicy("AllowAll", b =>
                {
                    b.AllowAnyOrigin()
                     .AllowAnyMethod()
                     .AllowAnyHeader();
                });
            });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDataAccessServices(connectionString);
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IReservationCartService, ReservationCartService>();
            builder.Services.AddScoped<ICustomerService, CustomerService>();
            builder.Services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // <-- allow HTTP locally
        options.SaveToken = false; // do not persist token server-side in auth middleware
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var auth = ctx.Request.Headers["Authorization"].FirstOrDefault();
                var present = !string.IsNullOrEmpty(auth);
                var startsWithBearer = present && auth!.StartsWith("Bearer ");
                Console.WriteLine($"[Jwt] OnMessageReceived - Authorization header present: {present}, startsWithBearer: {startsWithBearer}");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"[Jwt] AuthenticationFailed: {ctx.Exception?.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                Console.WriteLine($"[Jwt] TokenValidated for: {ctx.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                Console.WriteLine($"[Jwt] Challenge: error={ctx.Error}, desc={ctx.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });

            var app = builder.Build();

            // Seed data
            DataSeeding.SeedAsync(app.Services, builder.Configuration).GetAwaiter().GetResult();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // enable session (session stores only reservation cart state; no tokens or credentials)
            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
