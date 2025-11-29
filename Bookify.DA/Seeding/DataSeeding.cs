using Bookify.DA.Data;
using Bookify.DA.Entities;
using Bookify.DA.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Bookify.DA.Seeding
{
    public static class DataSeeding
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var logger = services.GetService<ILoggerFactory>()?.CreateLogger("DataSeeding");

            try
            {
                var db = services.GetRequiredService<AppDbContext>();
                // Remove if you prefer to run migrations manually.
                await db.Database.MigrateAsync();

                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

                var roles = new[] { "Admin", "Customer" };
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                        await roleManager.CreateAsync(new IdentityRole(role));
                }

                // admin in configuration (only attempt if both values provided)
                var adminEmail = configuration["AdminUser:Email"];
                var adminPassword = configuration["AdminUser:Password"];

                if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
                {
                    var admin = await userManager.FindByEmailAsync(adminEmail);
                    if (admin == null)
                    {
                        admin = new IdentityUser
                        {
                            UserName = adminEmail,
                            Email = adminEmail,
                            EmailConfirmed = true
                        };

                        var createResult = await userManager.CreateAsync(admin, adminPassword);
                        if (createResult.Succeeded)
                        {
                            await userManager.AddToRoleAsync(admin, "Admin");
                            logger?.LogInformation("Created admin user {Email}", adminEmail);
                        }
                        else
                        {
                            logger?.LogWarning("Failed to create admin user {Email}: {Errors}", adminEmail, string.Join("; ", createResult.Errors));
                        }
                    }
                    else
                    {
                        if (!await userManager.IsInRoleAsync(admin, "Admin"))
                            await userManager.AddToRoleAsync(admin, "Admin");
                    }
                }
                else
                {
                    logger?.LogInformation("Admin credentials not provided in configuration; skipping admin creation.");
                }

                // Seed domain data (RoomType, Hotel, Room)
                // Ensure there's at least one RoomType
                RoomType defaultRoomType = await db.RoomTypes.FirstOrDefaultAsync(rt => rt.Name == "Standard");
                if (defaultRoomType == null)
                {
                    defaultRoomType = new RoomType
                    {
                        Name = "Standard",
                        Description = "Standard room",
                        PricePerNight = 100m
                    };
                    await db.RoomTypes.AddAsync(defaultRoomType);
                    await db.SaveChangesAsync();
                    logger?.LogInformation("Seeded RoomType Id={Id}", defaultRoomType.Id);
                }

                // Ensure there's at least one Hotel
                Hotel defaultHotel = await db.Hotels.FirstOrDefaultAsync(h => h.Name == "Default Hotel");
                if (defaultHotel == null)
                {
                    defaultHotel = new Hotel
                    {
                        Name = "Default Hotel",
                        Location = "Default Address",
                        
                        ContactInfo = string.Empty
                    };
                    await db.Hotels.AddAsync(defaultHotel);
                    await db.SaveChangesAsync();
                    logger?.LogInformation("Seeded Hotel Id={Id}", defaultHotel.Id);
                }

                // Ensure there's at least one Room
                var existingRoom = await db.Rooms.FirstOrDefaultAsync(r => r.RoomNumber == "101" && r.HotelID == defaultHotel.Id);
                if (existingRoom == null)
                {
                    var defaultRoom = new Room
                    {
                        RoomNumber = "101",
                        HotelID = defaultHotel.Id,
                        RoomTypeID = defaultRoomType.Id,
                        Status = RoomStatus.Available
                    };
                    await db.Rooms.AddAsync(defaultRoom);
                    await db.SaveChangesAsync();
                    logger?.LogInformation("Seeded Room Id={Id}", defaultRoom.Id);
                }

                // Seed PaymentType "Stripe" if missing
                var stripeType = await db.PaymentTypes.FirstOrDefaultAsync(pt => pt.TypeName == "Stripe");
                if (stripeType == null)
                {
                    stripeType = new PaymentType
                    {
                        TypeName = "Stripe",
                        Description = "Stripe payments"
                    };

                    await db.PaymentTypes.AddAsync(stripeType);
                    await db.SaveChangesAsync();

                    logger?.LogInformation("Seeded PaymentType Stripe (Id={PaymentTypeId})", stripeType.Id);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error while seeding initial data.");
            }
        }
    }
}
