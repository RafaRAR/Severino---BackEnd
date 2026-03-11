using APIseverino.Models;
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
    public string Role { get; set; } = string.Empty;
    public string? ImagemUrl { get; set; }  // NOVO
    public int UsuarioId { get; set; }

    [JsonIgnore]
    public Usuario? Usuario { get; set; }
}