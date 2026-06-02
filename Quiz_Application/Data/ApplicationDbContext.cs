using Microsoft.EntityFrameworkCore;
using Quiz_Application.Models.Entities;

namespace Quiz_Application.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Question> Questions { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
    }
}
