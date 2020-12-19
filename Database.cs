using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace ItzChat
{
    public class ItzContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Messages> Messages { get; set; }
        public DbSet<Group> Groups { get; set; }

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
        public User Sender { get; set; }
        public User Receiver { get; set; }
        public string Content { get; set; }
    }
    public class Group
    {   
        public long Id { get; set; }
        public string Name { get; set; }
        public User Owner { get; set; }
        public List<User> Admins { get; set; }
        public List<User> Members { get; set; }
    }
}
