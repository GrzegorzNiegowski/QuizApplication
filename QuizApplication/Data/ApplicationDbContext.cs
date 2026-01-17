using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuizApplication.Models;

namespace QuizApplication.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Quiz>()
                .HasOne(q => q.Owner)
                .WithMany(u => u.Quizzes)
                .HasForeignKey(q => q.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Question -> Answers : kaskadowe usuwanie
            builder.Entity<Question>().HasMany(q => q.Answers).WithOne(a => a.Question).HasForeignKey(a => a.QuestionId).OnDelete(DeleteBehavior.Cascade);

            // Unikatowy index na AccessCode
            builder.Entity<Quiz>()
                .HasIndex(q => q.AccessCode)
                .IsUnique();

            // Dodatkowe limity długości (opcjonalnie)
            builder.Entity<Answer>().Property(a => a.Content).HasMaxLength(1000);
            builder.Entity<Question>().Property(q => q.Content).HasMaxLength(2000);
            builder.Entity<Quiz>().Property(q => q.Title).HasMaxLength(250);
        }

    }
}
