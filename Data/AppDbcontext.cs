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
        public DbSet<Post> Posts { get; set; }
        public DbSet<Cadastro> Cadastros { get; set; }

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

                entity.Property(u => u.EmailConfirmado)
                      .IsRequired()
                      .HasDefaultValue(false);

                entity.Property(u => u.CodigoVerificacao)
                      .HasMaxLength(16)
                      .IsUnicode(false)
                      .IsRequired(false);

                entity.Property(u => u.ExpiracaoVerificacao)
                      .IsRequired(false);

                entity.Property(u => u.CodigoResetSenha)
                      .HasMaxLength(16)
                      .IsUnicode(false)
                      .IsRequired(false);

                entity.Property(u => u.ExpiracaoResetSenha)
                      .IsRequired(false);
            });

            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Titulo)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(u => u.Contato)
       .IsRequired();

                entity.Property(u => u.Cep)
                     .IsRequired();

                entity.Property(u => u.Endereco)
                     .IsRequired();

                entity.Property(p => p.Conteudo)
                      .IsRequired();

                entity.Property(p => p.DataCriacao)
                      .HasDefaultValueSql("NOW()");

                entity.HasOne(p => p.Usuario)
                      .WithMany(u => u.Posts)
                      .HasForeignKey(p => p.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Cadastro>(static entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Cpf)
                      .HasMaxLength(20);

                entity.HasIndex(c => c.Cpf)
                    .IsUnique();

                entity.Property(c => c.DataNascimento)
                 .IsRequired();

                entity.Property(c => c.Contato)
                     .IsRequired();

                entity.Property(c => c.Cep)
                     .IsRequired();

                entity.Property(c => c.Endereco)
                     .IsRequired();

                entity.Property(c => c.Role)
                     .IsRequired();


                entity.HasOne(c => c.Usuario)
                      .WithOne(u => u.Cadastro)
                      .HasForeignKey<Cadastro>(c => c.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}