using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shopping.Models;
using System.Data;

namespace Shopping.Context
{
    public class ShoppingDb : DbContext
    {
        public ShoppingDb(DbContextOptions<ShoppingDb> options) : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<Setting> Settings => Set<Setting>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
        public DbSet<Coupon> Coupons => Set<Coupon>();
        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Banner> Banners => Set<Banner>();
        public DbSet<Comment> Comments => Set<Comment>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
            var userId = Guid.NewGuid();
            var salt = BCrypt.Net.BCrypt.GenerateSalt();

            modelBuilder.Entity<Admin>().HasData(
                new Admin
                {
                    Id = userId,
                    Email = "AdminUser@gmail.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("AdminUser", salt),
                    CreatedOn = DateTime.Now,
                    UserName = "User@",
                    FirstName = "Admin",
                    LastName = "Admin"

                }

            );


        }

    }
}
