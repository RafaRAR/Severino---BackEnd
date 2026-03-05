Endpoints:

# /api<br>

# ./Usuario<br>

# ../registrar<br>
## Parâmetros:<br>
nome - String<br>
email - String<br>
senha - String<br>

## Códigos:<br>
200 - OK ("Usuário criado")<br>
400 - Bad Request ("Email já existe")<br>

# ../login<br>
## Parâmetros:<br>
email - String<br>
senha - String<br>

## Códigos:<br>
200 - OK (token pra autenticação)<br>
401 - Unauthorized ("Usuário inválido", "Senha inválida")<br>

# ../verificar
## Parâmetros:<br>
email - String<br>
codigo - String<br>

## Códigos:<br>
200 - OK (token pra autenticação)<br>
400 - Bad Request ("Usuário não entrado", "Email já confirmado", "Nenhum código foi gerado para esse usuário", "Código expirado", "Código inválido")<br>

#../solicitarreset<br>
## Parâmetros:<br>
email - String<br>

## Códigos:<br>
200 - OK ({"message": "Código de reset enviado para o email."})<br>
400 - Bad Request ("Usuário não encontrado")<br>
500 - Internal Server Error ($"Falha ao enviar email: {ex.Message}")<br>

#../resetar<br>
## Parâmetros:<br>
email - String<br>
codigo - String<br>
novaSenha - String<br>

## Códigos:<br>
200 - OK ({"message": "Senha atualizada com sucesso."})
400 - Bad Request ("Usuário não entrado", "Email já confirmado", "Nenhum código foi gerado para esse usuário", "Código expirado", "Código inválido")<br>