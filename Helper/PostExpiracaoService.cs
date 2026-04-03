using APIseverino.Data;
using APIseverino.Models.Enums;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Serviço em background que roda a cada hora e marca como Expirado
/// todo post com Status = Aberto cuja DataExpiracao já passou.
/// </summary>
public class PostExpiracaoService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PostExpiracaoService> _logger;

    // Intervalo de verificação: 1 hora
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    public PostExpiracaoService(IServiceScopeFactory scopeFactory, ILogger<PostExpiracaoService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PostExpiracaoService iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpirarPostsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao expirar posts.");
            }

            await Task.Delay(Intervalo, stoppingToken);
        }
    }

    private async Task ExpirarPostsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var agora = DateTime.UtcNow;

        var postsExpirados = await context.Posts
            .Where(p => p.Status == StatusPost.Aberto && p.DataExpiracao < agora)
            .ToListAsync();

        if (postsExpirados.Count == 0)
            return;

        foreach (var post in postsExpirados)
            post.Status = StatusPost.Expirado;

        await context.SaveChangesAsync();

        _logger.LogInformation("{Count} post(s) marcado(s) como Expirado em {Agora}.",
            postsExpirados.Count, agora);
    }
}