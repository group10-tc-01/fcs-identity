# language: pt-br
Funcionalidade: Endpoints de autenticação
  Para acessar a plataforma Conexão Solidária
  Como usuário da API de identidade
  Quero registrar doadores e autenticar usuários

Cenário: Registrar doador com sucesso
  Dado que tenho uma requisição válida para registrar um doador
  Quando eu enviar a requisição para registrar o doador
  Então a resposta deve ter status 201
  E a resposta deve indicar sucesso
  E a resposta deve conter os dados do doador registrado

Cenário: Autenticar usuário com sucesso
  Dado que tenho uma requisição válida de login
  Quando eu enviar a requisição de login
  Então a resposta deve ter status 200
  E a resposta deve indicar sucesso
  E a resposta deve conter o token de acesso

Cenário: Recusar login com credenciais inválidas
  Dado que o provedor de identidade recusará as credenciais
  E que tenho uma requisição de login com credenciais inválidas
  Quando eu enviar a requisição de login
  Então a resposta deve ter status 401
  E a resposta deve indicar falha
  E a mensagem da resposta deve ser "Invalid email or password."

Cenário: Recusar registro de doador inválido
  Dado que tenho uma requisição inválida para registrar um doador
  Quando eu enviar a requisição para registrar o doador
  Então a resposta deve ter status 400
  E a resposta deve indicar falha
