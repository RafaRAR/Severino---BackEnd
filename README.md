Endpoints:

# /api

# ./Usuario

# ../registrar
## Parâmetros:
nome - String<br>
email - String<br>
senha - String<br>

## Códigos:
200 - OK ("Usuário criado")<br>
400 - Bad Request ("Email já existe")<br>

# ../api/login
## Parâmetros:
email - String<br>
senha - String<br>

## Códigos:
200 - OK (token pra autenticação)<br>
401 - Unauthorized ("Usuário inválido", "Senha inválida")<br>
