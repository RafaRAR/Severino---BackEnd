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
        public DbSet<Lance> Lances { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Cadastro> Cadastros { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<PostImagem> PostImagens { get; set; }
        public DbSet<Verificacao> Verificacoes { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ─── Usuario ──────────────────────────────────────────────────────────────
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

            // ─── Post ─────────────────────────────────────────────────────────────────
            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Titulo)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(p => p.Contato)
                      .IsRequired();

                entity.Property(p => p.Cep)
                      .IsRequired();

                entity.Property(p => p.Endereco)
                      .IsRequired();

                entity.Property(p => p.Conteudo)
                      .IsRequired();

                entity.Property(c => c.Role)
                      .IsRequired();

                entity.Property(p => p.DataCriacao)
                      .HasDefaultValueSql("NOW()");

                entity.Property(p => p.DataExpiracao)
                      .IsRequired();

                entity.Property(p => p.Impulsionar)
                      .HasDefaultValue(false);

                entity.Property(p => p.Status)
                      .IsRequired()
                      .HasConversion<int>()
                      .HasDefaultValue(StatusPost.Aberto);

                entity.Property(p => p.PrestadorEmNegociacaoId)
                      .IsRequired(false);

                entity.HasOne(p => p.Usuario)
                      .WithMany(u => u.Posts)
                      .HasForeignKey(p => p.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── Cadastro ─────────────────────────────────────────────────────────────
            modelBuilder.Entity<Cadastro>(entity =>
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

                entity.HasOne(c => c.Usuario)
                      .WithOne(u => u.Cadastro)
                      .HasForeignKey<Cadastro>(c => c.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(c => c.prestadorVerificado)
                      .IsRequired()
                      .HasDefaultValue(false);
            });

            // ─── Tag ──────────────────────────────────────────────────────────────────
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Nome)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.HasIndex(t => t.Nome)
                      .IsUnique();
            });

            modelBuilder.Entity<Post>()
                .HasMany(p => p.Tags)
                .WithMany(t => t.Posts)
                .UsingEntity(j => j.ToTable("PostTags"));

            // ─── Comentario ───────────────────────────────────────────────────────────
            modelBuilder.Entity<Comentario>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Conteudo)
                      .IsRequired();

                entity.Property(c => c.ValorDeLance)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0m)
                      .IsRequired();

                entity.Property(c => c.DataCriacao)
                      .HasDefaultValueSql("NOW()");

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

            // ─── PostImagem ───────────────────────────────────────────────────────────
            modelBuilder.Entity<PostImagem>(entity =>
            {
                entity.HasKey(i => i.Id);

                entity.Property(i => i.Url)
                      .IsRequired();

                entity.Property(i => i.FileId)
                      .IsRequired(false);

                entity.HasOne(i => i.Post)
                      .WithMany(p => p.Imagens)
                      .HasForeignKey(i => i.PostId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── Verificacao ──────────────────────────────────────────────────────────
            modelBuilder.Entity<Verificacao>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.Property(v => v.ImagemUrl)
                      .IsRequired();

                entity.Property(v => v.ImagemFileId)
                      .IsRequired(false);

                entity.Property(v => v.Situacao)
                      .IsRequired()
                      .HasConversion<int>()
                      .HasDefaultValue(SituacaoVerificacao.Aguardando);

                entity.Property(v => v.DataSolicitacao)
                      .HasDefaultValueSql("NOW()");

                entity.Property(v => v.DataAvaliacao)
                      .IsRequired(false);

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

            // ─── ChatRoom ─────────────────────────────────────────────────────────────
            modelBuilder.Entity<ChatRoom>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.DataCriacao)
                      .HasDefaultValueSql("NOW()");

                // Um Post pode ter múltiplas salas (uma por par cliente/prestador)
                entity.HasOne(r => r.Post)
                      .WithMany(p => p.ChatRooms)
                      .HasForeignKey(r => r.PostId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Cliente)
                      .WithMany()
                      .HasForeignKey(r => r.ClienteId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Prestador)
                      .WithMany()
                      .HasForeignKey(r => r.PrestadorId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Garante que exista apenas 1 sala por combinação Post+Cliente+Prestador
                entity.HasIndex(r => new { r.PostId, r.ClienteId, r.PrestadorId })
                      .IsUnique();
            });

            // ─── ChatMessage ──────────────────────────────────────────────────────────
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.Property(m => m.Conteudo)
                      .IsRequired();

                entity.Property(m => m.SenderNome)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(m => m.DataEnvio)
                      .HasDefaultValueSql("NOW()");

                entity.HasOne(m => m.ChatRoom)
                      .WithMany(r => r.Mensagens)
                      .HasForeignKey(m => m.ChatRoomId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── Lance ────────────────────────────────────────────────────────────────
            modelBuilder.Entity<Lance>(entity =>
            {
                entity.HasKey(l => l.Id);

                entity.Property(l => l.ValorDeLance)
                      .HasColumnType("decimal(18,2)")
                      .IsRequired();

                entity.Property(l => l.IsAccepted)
                      .HasDefaultValue(false);

                entity.Property(l => l.DataCriacao)
                      .HasDefaultValueSql("NOW()");

                // Relação com o Post (Cascade: se deletar o post, deleta os lances)
                entity.HasOne(l => l.Post)
                      .WithMany(p => p.Lances)
                      .HasForeignKey(l => l.IdPost)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relação com o Prestador (Restrict: impede de deletar o usuário se ele tiver lances ativos, evitando bugs)
                entity.HasOne(l => l.Prestador)
                      .WithMany()
                      .HasForeignKey(l => l.IdPrestadorResponsavel)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}