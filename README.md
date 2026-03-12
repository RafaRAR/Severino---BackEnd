Endpoints:

# /api<br>


# ./usuario<br>


# ../registrar<br>
## Parâmetros:<br>
nome - String<br>
email - String<br>
senha - String<br>

## Códigos:<br>
200 - OK ("Código de verificação enviado para o email.")<br>
400 - Bad Request ("Email já existe")<br>


# ../login<br>
## Parâmetros:<br>
email - String<br>
senha - String<br>

## Códigos:<br>
200 - OK (token pra autenticação)<br>
401 - Unauthorized ("Usuário inválido", "Senha inválida")<br>


# ../verificar<br>
## Parâmetros:<br>
email - String<br>
codigo - String<br>

## Códigos:<br>
200 - OK (token pra autenticação)<br>
400 - Bad Request ("Usuário não entrado", "Email já confirmado", "Nenhum código foi gerado para esse usuário", "Código expirado", "Código inválido")<br>


# ../solicitarverificacao<br>
## Parâmetros<br>
email - String<br>

## Códigos:<br>
200 - OK ("Código de verificação enviado para o email.")<br>
400 - Bad Request ("Usuário não encontrado", "Email já confirmado")<br>
500 - Internal Server Error ($"Falha ao enviar email: {ex.Message}")<br>


# ../solicitarreset<br>
## Parâmetros:<br>
email - String<br>

## Códigos:<br>
200 - OK ({"message": "Código de reset enviado para o email."})<br>
400 - Bad Request ("Usuário não encontrado")<br>
500 - Internal Server Error ($"Falha ao enviar email: {ex.Message}")<br>


# ../resetar<br>
## Parâmetros:<br>
email - String<br>
codigo - String<br>
novaSenha - String<br>

## Códigos:<br>
200 - OK ({"message": "Senha atualizada com sucesso."})<br>
400 - Bad Request ("Usuário não entrado", "Email já confirmado", "Nenhum código foi gerado para esse usuário", "Código expirado", "Código inválido")<br>


# ../deletar/{id}<br>
## Códigos:<br>
200 - OK ({"message": "Email de confirmação enviado. Verifique sua caixa de entrada."})<br>
404 - Not Found ("Usuário não encontrado")<br>
500 - Internal Server Error ($"Falha ao enviar email: {ex.Message}")<br>


# ../confirmardeletar/{id}<br>
## Parâmetros:<br>
email - String<br>
codigo - String<br>

## Códigos:<br>
200 - OK ({"message": "Usuário deletado com sucesso."})<br>
400 - Bad Request ("Usuário não entrado", "Nenhum código foi gerado para esse usuário", "Código expirado", "Código inválido")<br>


# ../getidbyemail<br>
## Parâmetros:<br>
email - String<br>

## Códigos:<br>
200 - OK ({usuario.Id})<br>


# ../getall<br>
## Códigos:<br>
200 - OK ({_context.Usuarios.Select(u => new { u.Id, u.Nome, u.Email }).ToListAsync()})<br>


# ./cadastro<br>


# ../cadastrar/{usuario.id}<br>
## Parâmetros:<br>
nome - String
usuarioId - int
cpf - String
dataNascimento - dateTime.ToString("yyyy-MM-dd"),
contato - String
cep - String
endereco - String
role - String

## Códigos:<br>
200 - OK (cadastro)<br>
400 - Bad Request ("Usuário não encontrado", "CPF já cadastrado")<br>


# ../getcadastro/{usuarioid}
## Códigos:<br>
200 - OK ({cadastro})<br>


# ../updatecadastro/{usuarioid}<br>
## Parâmetros:<br>
nome - String
cpf - String
dataNascimento - dateTime.ToString("yyyy-MM-dd"),
contato - String
cep - String
endereco - String
role - String

## Códigos:<br>
200 - OK (cadastro)<br>
400 - Bad Request ("Usuário não encontrado")<br>


# ./post<br>


# ../postar/{usuarioid}<br>
## Parâmetros:<br>
titulo - String<br>
conteudo - String<br>
endereco - String<br>
cep - String<br>
contato - String<br>
imageUrl - String<br>


## Códigos:<br>
200 - OK (post)<br>
400 - Bad Request ("Usuário não encontrado", "Erro ao fazer upload da imagem: {ex.Message}")<br>


# ../getposts<br>
## Códigos:<br>
200 - OK ({_context.Posts.Select(p => new { p.Id, p.Titulo, p.Conteudo, p.Endereco, p.Cep, p.Contato, p.ImageUrl, p.UsuarioId }).ToListAsync()})<br>


# ../getposts/usuario/{usuarioid}<br>
## Códigos:<br>
200 - OK ({_context.Posts.Where(p => p.UsuarioId == usuarioid).Select(p => new { p.Id, p.Titulo, p.Conteudo, p.Endereco, p.Cep, p.Contato, p.ImageUrl }).ToListAsync()})<br>


# ../editar/{idpost}<br>
## Parâmetros:<br>
titulo - String<br>
conteudo - String<br>
endereco - String<br>
cep - String<br>
contato - String<br>

## Códigos:<br>
200 - OK (post)<br>
400 - Bad Request ("Post não encontrado")<br>


# ../deletar/{idpost}<br>
## Códigos:<br>
200 - OK ({"message": "Post deletado com sucesso."})<br>
400 - Bad Request ("Post não encontrado")<br>