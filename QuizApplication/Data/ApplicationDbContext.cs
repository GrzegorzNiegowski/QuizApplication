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
            // Quiz -> Właściciel
            builder.Entity<Quiz>()
                .HasOne(q => q.Owner)
                .WithMany(u => u.Quizzes)
                .HasForeignKey(q => q.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Quiz -> Pytania (kaskadowe usuwanie)
            builder.Entity<Quiz>()
                .HasMany(q => q.Questions)
                .WithOne(q => q.Quiz)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            // Pytanie -> Odpowiedzi (kaskadowe usuwanie)
            builder.Entity<Question>()
                .HasMany(q => q.Answers)
                .WithOne(a => a.Question)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unikalny indeks na AccessCode
            builder.Entity<Quiz>()
                .HasIndex(q => q.AccessCode)
                .IsUnique();

            // Limity długości
            builder.Entity<Quiz>().Property(q => q.Title).HasMaxLength(200);
            builder.Entity<Quiz>().Property(q => q.AccessCode).HasMaxLength(6);
            builder.Entity<Question>().Property(q => q.Content).HasMaxLength(2000);
            builder.Entity<Answer>().Property(a => a.Content).HasMaxLength(1000);
        }

    }
}
