using APIseverino.Models;
using APIseverino.Models.Enums;
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
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<PostImagem> PostImagens { get; set; }
        public DbSet<Verificacao> Verificacoes { get; set; }

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
                      .HasColumnName("SenhaHash")
                      .IsRequired();

                entity.Property(u => u.SenhaSalt)
                      .HasColumnName("SenhaSalt")
                      .IsRequired();

                entity.Property(u => u.EmailConfirmado)
                      .HasColumnName("EmailConfirmado")
                      .IsRequired()
                      .HasDefaultValue(false);

                entity.Property(u => u.CodigoVerificacao)
                      .HasColumnName("CodigoVerificacao")
                      .HasMaxLength(16)
                      .IsUnicode(false)
                      .IsRequired(false);

                entity.Property(u => u.ExpiracaoVerificacao)
                      .HasColumnName("ExpiracaoVerificacao")
                      .IsRequired(false);

                entity.Property(u => u.CodigoResetSenha)
                      .HasColumnName("CodigoResetSenha")
                      .HasMaxLength(16)
                      .IsUnicode(false)
                      .IsRequired(false);

                entity.Property(u => u.ExpiracaoResetSenha)
                      .HasColumnName("ExpiracaoResetSenha")
                      .IsRequired(false);

                entity.Property(u => u.CodigoDelete)
                      .HasColumnName("CodigoDelete")
                      .IsRequired(false);

                entity.Property(u => u.ExpiracaoDelete)
                      .HasColumnName("ExpiracaoDelete")
                      .IsRequired(false);
            });

            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Titulo)
                      .HasColumnName("Titulo")
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(p => p.Contato)
                      .HasColumnName("Contato")
                      .IsRequired();

                entity.Property(p => p.Cep)
                      .HasColumnName("Cep")
                      .IsRequired();

                entity.Property(p => p.Endereco)
                      .HasColumnName("Endereco")
                      .IsRequired();

                entity.Property(p => p.Conteudo)
                      .HasColumnName("Conteudo")
                      .IsRequired();

                entity.Property(p => p.Role)
                      .HasColumnName("Role")
                      .IsRequired();

                entity.Property(p => p.DataCriacao)
                      .HasColumnName("DataCriacao")
                      .HasDefaultValueSql("NOW()");

                entity.Property(p => p.Impulsionar)
                      .HasColumnName("Impulsionar")
                      .HasDefaultValue(false);

                entity.Property(p => p.UsuarioId)
                      .HasColumnName("UsuarioId");

                entity.HasOne(p => p.Usuario)
                      .WithMany(u => u.Posts)
                      .HasForeignKey(p => p.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Cadastro>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Cpf)
                      .HasColumnName("Cpf")
                      .HasMaxLength(20);

                entity.HasIndex(c => c.Cpf)
                      .IsUnique();

                entity.Property(c => c.Nome)
                      .HasColumnName("Nome");

                entity.Property(c => c.DataNascimento)
                      .HasColumnName("DataNascimento")
                      .IsRequired();

                entity.Property(c => c.Contato)
                      .HasColumnName("Contato")
                      .IsRequired();

                entity.Property(c => c.Cep)
                      .HasColumnName("Cep")
                      .IsRequired();

                entity.Property(c => c.Endereco)
                      .HasColumnName("Endereco")
                      .IsRequired();

                entity.Property(c => c.ImagemUrl)
                      .HasColumnName("ImagemUrl");

                entity.Property(c => c.ImagemFileId)
                      .HasColumnName("ImagemFileId");

                entity.Property(c => c.TipoUsuario)
                      .HasColumnName("TipoUsuario");

                entity.Property(c => c.prestadorVerificado)
                      .HasColumnName("prestadorVerificado")
                      .IsRequired()
                      .HasDefaultValue(false);

                entity.Property(c => c.UsuarioId)
                      .HasColumnName("UsuarioId");

                entity.HasOne(c => c.Usuario)
                      .WithOne(u => u.Cadastro)
                      .HasForeignKey<Cadastro>(c => c.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Nome)
                      .HasColumnName("Nome")
                      .IsRequired()
                      .HasMaxLength(100);
                entity.HasIndex(t => t.Nome)
                      .IsUnique();
            });

            modelBuilder.Entity<Post>()
                .HasMany(p => p.Tags)
                .WithMany(t => t.Posts)
                .UsingEntity(j => j.ToTable("PostTags"));

            modelBuilder.Entity<Comentario>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Conteudo)
                      .HasColumnName("Conteudo")
                      .IsRequired();

                entity.Property(c => c.ValorDeLance)
                      .HasColumnName("ValorDeLance")
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0m)
                      .IsRequired();

                entity.Property(c => c.DataCriacao)
                      .HasColumnName("DataCriacao")
                      .HasDefaultValueSql("NOW()");

                entity.Property(c => c.UsuarioId)
                      .HasColumnName("UsuarioId");

                entity.Property(c => c.PostId)
                      .HasColumnName("PostId");

                entity.HasOne(c => c.Usuario)
                      .WithMany(u => u.Comentarios)
                      .HasForeignKey(c => c.UsuarioId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Post)
                      .WithMany(p => p.Comentarios)
                      .HasForeignKey(c => c.PostId)
                      .IsRequired()
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // PostImagem: tabela de imagens por post
            modelBuilder.Entity<PostImagem>(entity =>
            {
                entity.HasKey(i => i.Id);

                entity.Property(i => i.Url)
                      .HasColumnName("Url")
                      .IsRequired();

                entity.Property(i => i.FileId)
                      .HasColumnName("FileId")
                      .IsRequired(false);

                entity.Property(i => i.PostId)
                      .HasColumnName("PostId");

                entity.HasOne(i => i.Post)
                      .WithMany(p => p.Imagens)
                      .HasForeignKey(i => i.PostId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Verificacao: solicitações de verificação de prestador
            // ATENÇÃO: o banco usa "UsuarioId" como FK para Cadastro (não CadastroId)
            modelBuilder.Entity<Verificacao>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.Property(v => v.ImagemUrl)
                      .HasColumnName("ImagemUrl")
                      .IsRequired();

                entity.Property(v => v.ImagemFileId)
                      .HasColumnName("ImagemFileId")
                      .IsRequired(false);

                entity.Property(v => v.Situacao)
                      .HasColumnName("Situacao")
                      .IsRequired()
                      .HasConversion<int>()
                      .HasDefaultValue(SituacaoVerificacao.Aguardando);

                entity.Property(v => v.DataSolicitacao)
                      .HasColumnName("DataSolicitacao")
                      .HasDefaultValueSql("NOW()");

                entity.Property(v => v.DataAvaliacao)
                      .HasColumnName("DataAvaliacao")
                      .IsRequired(false);

                // No banco a coluna se chama "UsuarioId", não "CadastroId"
                entity.Property(v => v.CadastroId)
                      .HasColumnName("UsuarioId");

                entity.Property(v => v.UpdatedById)
                      .HasColumnName("UpdatedById");

                entity.HasOne(v => v.Cadastro)
                      .WithOne(c => c.Verificacao)
                      .HasForeignKey<Verificacao>(v => v.CadastroId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(v => v.UpdatedBy)
                      .WithMany()
                      .HasForeignKey(v => v.UpdatedById)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}