using System;
using zedcrest.api.Models;
using Microsoft.EntityFrameworkCore;

namespace zedcrest.api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options){}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           modelBuilder.Entity<User>().ToTable("User");
           modelBuilder.Entity<Upload>().ToTable("Upload");

           modelBuilder.Entity<Upload>()
                       .HasOne(u =>u.User)
                       .WithMany(u =>u.UserUploads)
                       .OnDelete(DeleteBehavior.ClientCascade);

            base.OnModelCreating(modelBuilder);
        }

        //dbsets
        public DbSet<User> Users { get; set; }
        public DbSet<Upload> Uploads { get; set; }
    }
}