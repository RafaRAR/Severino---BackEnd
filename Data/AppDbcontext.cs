using APIseverino.Models;
using Microsoft.EntityFrameworkCore;

namespace APIseverino.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Nome)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(u => u.Email)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.HasIndex(u => u.Email)
                      .IsUnique();

                entity.Property(u => u.SenhaHash)
                      .IsRequired();

                entity.Property(u => u.SenhaSalt)
                      .IsRequired();

                // Email confirmado
                entity.Property(u => u.EmailConfirmado)
                      .IsRequired()
                      .HasDefaultValue(false);

                // Código de verificação da conta
                entity.Property(u => u.CodigoVerificacao)
                      .HasMaxLength(16)
                      .IsUnicode(false)
                      .IsRequired(false);

                entity.Property(u => u.ExpiracaoVerificacao)
                      .IsRequired(false);

                // Código de reset de senha
                entity.Property(u => u.CodigoResetSenha)
                      .HasMaxLength(16)
                      .IsUnicode(false)
                      .IsRequired(false);

                entity.Property(u => u.ExpiracaoResetSenha)
                      .IsRequired(false);
            });
        }
    }
}