Endpoints:

/api

./Usuario

../registrar
Parâmetros:
nome - String
email - String
senha - String

Códigos:
200 - OK ("Usuário criado")
400 - Bad Request ("Email já existe")

../api/login
Parâmetros:
email - String
senha - String

Códigos:
200 - OK (token pra autenticação)
401 - Unauthorized ("Usuário inválido", "Senha inválida")

