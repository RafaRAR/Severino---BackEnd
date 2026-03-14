using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIseverino.Models;

public class Usuario
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public byte[] SenhaHash { get; set; } = null!;

    public byte[] SenhaSalt { get; set; } = null!;

    public bool EmailConfirmado { get; set; } = false;

    public string? CodigoVerificacao { get; set; }

    public DateTime? ExpiracaoVerificacao { get; set; }

    public string? CodigoResetSenha { get; set; }

    public DateTime? ExpiracaoResetSenha { get; set; }

    public string? CodigoDelete { get; set; }

    public DateTime? ExpiracaoDelete { get; set; }

    public List<Post>? Posts { get; set; }

    public Cadastro? Cadastro { get; set; }
}