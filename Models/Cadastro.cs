using APIseverino.Models;
using APIseverino.Models.Enums;
using System.Text.Json.Serialization;

public class Cadastro
{
    public int Id { get; set; }

    public string Cpf { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string DataNascimento { get; set; } = string.Empty;
    public string Contato { get; set; } = string.Empty;
    public string Cep { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string? ImagemUrl { get; set; }
    public string? ImagemFileId { get; set; }

    public TipoUsuario TipoUsuario { get; set; } = TipoUsuario.Cliente;
    public bool prestadorVerificado { get; set; } = false;

    public int UsuarioId { get; set; }

    [JsonIgnore]
    public Usuario? Usuario { get; set; }

    [JsonIgnore]
    public Verificacao? Verificacao { get; set; }
}