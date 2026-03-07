using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIseverino.Models;

public class Usuario
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public byte[] SenhaHash { get; set; }

    public byte[] SenhaSalt { get; set; }

    public bool EmailConfirmado { get; set; } = false;

    public string? CodigoVerificacao { get; set; }

    public DateTime? ExpiracaoVerificacao { get; set; }

    public string? CodigoResetSenha { get; set; }

    public DateTime? ExpiracaoResetSenha { get; set; }

    
    public List<Post> Posts { get; set; } = new();

    public Cadastro? Cadastro { get; set; }
}