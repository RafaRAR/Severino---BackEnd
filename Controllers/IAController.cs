using APIseverino.Data;
using APIseverino.Models;
using APIseverino.Models.Enums;
using APIseverino.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace APIseverino.Controllers
{
    [ApiController]
    [Route("api/ia")]
    public class IAController : ControllerBase
    {
        private readonly OpenAIService _openAI;
        private readonly AppDbContext _context;

        public IAController(
            OpenAIService openAI,
            AppDbContext context
        )
        {
            _openAI = openAI;
            _context = context;
        }

        // POST: api/ia/gerar-post/1
        [HttpPost("gerar-post/{usuarioId}")]
        public async Task<IActionResult> GerarPost(
            int usuarioId,
            [FromBody] string descricao
        )
        {
            try
            {
                // USUÁRIO
                var usuario = await _context.Usuarios
                    .Include(u => u.Cadastro)
                    .FirstOrDefaultAsync(u => u.Id == usuarioId);

                if (usuario == null)
                    return BadRequest("Usuário não encontrado");

                // TAGS DISPONÍVEIS
                var tagsDisponiveis = await _context.Tags
                    .Select(t => new
                    {
                        t.Id,
                        t.Nome
                    })
                    .ToListAsync();

                var tagsTexto = string.Join(
                    "\n",
                    tagsDisponiveis.Select(t =>
                        $"ID: {t.Id} - {t.Nome}"
                    )
                );

                // BUSCA CONVERSA ATIVA
                var conversa = await _context.IAConversations
                    .Include(c => c.Mensagens)
                    .FirstOrDefaultAsync(c =>
                        c.UsuarioId == usuarioId &&
                        !c.Finalizada
                    );

                // CRIA NOVA CONVERSA
                if (conversa == null)
                {
                    conversa = new IAConversation
                    {
                        UsuarioId = usuarioId,
                        Finalizada = false
                    };

                    _context.IAConversations.Add(conversa);

                    await _context.SaveChangesAsync();
                }

                // GARANTE LISTA
                conversa.Mensagens ??= new List<IAMensagem>();

                // SALVA MENSAGEM DO USUÁRIO
                conversa.Mensagens.Add(new IAMensagem
                {
                    IAConversationId = conversa.Id,
                    Role = "user",
                    Conteudo = descricao,
                    DataEnvio = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // PROMPT
                var prompt = @$"
Você é uma inteligência artificial responsável EXCLUSIVAMENTE
por criar posts para uma plataforma de contratação de serviços.

Você NÃO é um chatbot comum.

Você NÃO pode conversar sobre assuntos fora do contexto
de criação de posts de serviço.

Seu único objetivo é:
- entender a necessidade do usuário
- coletar informações essenciais
- criar um post profissional
- classificar corretamente as categorias

REGRAS ABSOLUTAS:

- Nunca saia do contexto de serviços.
- Nunca responda perguntas pessoais.
- Nunca converse casualmente.
- Nunca dê opiniões.
- Nunca conte piadas.
- Nunca explique seu funcionamento.
- Nunca responda temas externos.
- Nunca altere o foco da conversa.

Se o usuário enviar algo fora do contexto,
retorne:

{{
  ""foraDeContexto"": true,
  ""mensagem"": ""Descreva o serviço que você precisa para que eu possa criar seu post.""
}}

Seu objetivo é criar um post COMPLETO
com a MENOR quantidade possível de perguntas.

REGRAS DE PERGUNTAS:

- Faça apenas perguntas ESSENCIAIS.
- Nunca faça perguntas desnecessárias.
- Nunca interrogue excessivamente.
- Faça no máximo 3 perguntas por vez.
- Se já houver informação suficiente,
  gere o post imediatamente.
- Tente inferir o máximo possível pelo contexto.

Você deve preencher:

- titulo
- conteudo
- role
- impulsionar
- endereco
- cep
- contato
- tagIds

NÃO invente informações.

Pergunte apenas:
- endereço
- telefone
- CEP
- urgência
- tipo do problema
- detalhes técnicos importantes

Quando faltar informação importante, retorne:

{{
  ""precisaMaisInformacoes"": true,
  ""perguntas"": [
    ""Pergunta 1"",
    ""Pergunta 2""
  ]
}}

Quando já houver informações suficientes, retorne:

{{
  ""precisaMaisInformacoes"": false,
  ""titulo"": """",
  ""conteudo"": """",
  ""role"": ""Cliente"",
  ""impulsionar"": false,
  ""endereco"": """",
  ""cep"": """",
  ""contato"": """",
  ""tagIds"": [1]
}}

REGRAS DO POST:

- O título deve ser curto,
  natural e objetivo.

- O conteúdo NÃO deve repetir o título.

- Evite redundância.

- Não reescreva a mesma informação
  de formas diferentes.

- O conteúdo deve complementar o título.

- Escreva como uma pessoa real pediria o serviço.

- Evite linguagem robótica.

- Evite textos genéricos.

- NÃO repita endereço,
  telefone ou CEP no conteúdo.

- O texto deve parecer escrito rapidamente
  por um usuário comum.

- Evite linguagem comercial.

- Não escreva propaganda.

- Escolha categorias específicas.

- Só utilize múltiplas tags
  quando realmente fizer sentido.

- Nunca deixe campos vazios.
- Nunca retorne null.
- CEP deve conter apenas números.
- Contato deve conter apenas números.

- Escolha SOMENTE IDs reais das categorias.

- Não use markdown.
- Não explique nada.

- Retorne APENAS JSON válido.

Categorias disponíveis:

{tagsTexto}
";

                // HISTÓRICO
                var historico = conversa.Mensagens
                    .OrderBy(m => m.DataEnvio)
                    .TakeLast(15)
                    .Select(m =>
                        $"{m.Role}: {m.Conteudo}"
                    );

                // CONTEXTO FINAL
                var contextoCompleto =
                    prompt +
                    "\n\nHistórico da conversa:\n" +
                    string.Join("\n", historico);

                // CHAMA IA
                var respostaIA =
                    await _openAI.GerarTexto(contextoCompleto);

                // LIMPA POSSÍVEL MARKDOWN
                respostaIA = respostaIA
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                // SALVA RESPOSTA IA
                conversa.Mensagens.Add(new IAMensagem
                {
                    IAConversationId = conversa.Id,
                    Role = "assistant",
                    Conteudo = respostaIA,
                    DataEnvio = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // PARSE JSON
                using var documento =
                    JsonDocument.Parse(respostaIA);

                var root = documento.RootElement;

                // FORA DE CONTEXTO
                if (
                    root.TryGetProperty(
                        "foraDeContexto",
                        out var foraContexto
                    )
                    &&
                    foraContexto.GetBoolean()
                )
                {
                    return Ok(new
                    {
                        foraDeContexto = true,
                        mensagem = root
                            .GetProperty("mensagem")
                            .GetString()
                    });
                }

                // MAIS INFORMAÇÕES
                if (
                    root.TryGetProperty(
                        "precisaMaisInformacoes",
                        out var precisaInfo
                    )
                    &&
                    precisaInfo.GetBoolean()
                )
                {
                    return Ok(new
                    {
                        precisaMaisInformacoes = true,
                        perguntas = root
                            .GetProperty("perguntas")
                            .EnumerateArray()
                            .Select(p => p.GetString())
                            .ToList()
                    });
                }

                // DESERIALIZA
                var dto = JsonSerializer.Deserialize<IAResponsePost>(
                    respostaIA,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );

                if (dto == null)
                {
                    return BadRequest(new
                    {
                        erro = "Erro ao interpretar resposta da IA",
                        respostaIA
                    });
                }

                // DADOS FINAIS
                var enderecoFinal =
                    string.IsNullOrWhiteSpace(dto.Endereco)
                        ? usuario.Cadastro?.Endereco
                        : dto.Endereco;

                var cepFinal =
                    string.IsNullOrWhiteSpace(dto.Cep)
                        ? usuario.Cadastro?.Cep
                        : dto.Cep;

                var contatoFinal =
                    string.IsNullOrWhiteSpace(dto.Contato)
                        ? usuario.Cadastro?.Contato
                        : dto.Contato;

                // VALIDAÇÕES
                var perguntasFaltando = new List<string>();

                if (string.IsNullOrWhiteSpace(dto.Titulo))
                {
                    perguntasFaltando.Add(
                        "Qual seria um título curto para o serviço?"
                    );
                }

                if (string.IsNullOrWhiteSpace(dto.Conteudo))
                {
                    perguntasFaltando.Add(
                        "Pode descrever melhor o problema ou serviço?"
                    );
                }

                if (dto.TagIds == null || !dto.TagIds.Any())
                {
                    perguntasFaltando.Add(
                        "Qual categoria melhor representa o serviço?"
                    );
                }

                if (string.IsNullOrWhiteSpace(enderecoFinal))
                {
                    perguntasFaltando.Add(
                        "Qual o endereço do serviço?"
                    );
                }

                if (string.IsNullOrWhiteSpace(cepFinal))
                {
                    perguntasFaltando.Add(
                        "Qual o CEP do local?"
                    );
                }

                if (string.IsNullOrWhiteSpace(contatoFinal))
                {
                    perguntasFaltando.Add(
                        "Qual telefone para contato?"
                    );
                }

                // SE FALTAR ALGO
                if (perguntasFaltando.Any())
                {
                    return Ok(new
                    {
                        precisaMaisInformacoes = true,
                        perguntas = perguntasFaltando.Take(3)
                    });
                }

                // CRIA POST
                var post = new Post
                {
                    UsuarioId = usuarioId,

                    Titulo = dto.Titulo.Trim(),

                    Conteudo = dto.Conteudo.Trim(),

                    Role = string.IsNullOrWhiteSpace(dto.Role)
                        ? "Cliente"
                        : dto.Role,

                    DataCriacao = DateTime.UtcNow,

                    DataExpiracao =
                        DateTime.UtcNow.AddDays(20),

                    Status = StatusPost.Aberto,

                    Impulsionar = dto.Impulsionar,

                    Endereco = enderecoFinal.Trim(),

                    Cep = new string(
                        cepFinal.Where(char.IsDigit).ToArray()
                    ),

                    Contato = new string(
                        contatoFinal.Where(char.IsDigit).ToArray()
                    )
                };

                _context.Posts.Add(post);

                await _context.SaveChangesAsync();

                // TAGS
                if (dto.TagIds != null && dto.TagIds.Any())
                {
                    var tags = await _context.Tags
                        .Where(t => dto.TagIds.Contains(t.Id))
                        .ToListAsync();

                    post.Tags ??= new List<Tag>();

                    foreach (var tag in tags)
                    {
                        post.Tags.Add(tag);
                    }

                    await _context.SaveChangesAsync();
                }

                // FINALIZA CONVERSA
                conversa.Finalizada = true;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    precisaMaisInformacoes = false,
                    mensagem = "Post criado com sucesso",
                    tagsSelecionadas = dto.TagIds,
                    post
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    erro = ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace
                });
            }
        }
    }

    public class IAResponsePost
    {
        public bool PrecisaMaisInformacoes { get; set; }

        public string Titulo { get; set; }

        public string Conteudo { get; set; }

        public string Role { get; set; }

        public bool Impulsionar { get; set; }

        public string Endereco { get; set; }

        public string Cep { get; set; }

        public string Contato { get; set; }

        public List<int> TagIds { get; set; }
    }
}