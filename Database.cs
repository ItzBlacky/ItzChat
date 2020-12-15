using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ItzChat
{
    public class ItzContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Messages> Messages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder builder) =>
            builder.UseSqlite("Data Source=database.db");
    }
    public class User
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class Messages
    {
        public long Id { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string message { get; set; }
    }
}
