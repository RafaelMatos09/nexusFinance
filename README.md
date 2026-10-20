# Nexus Finance

Plataforma fictícia de gestão financeira e cobrança para PMEs. Projeto de portfólio. Todos os dados são fictícios.

## Stack

- Backend: C# .NET 8, Web API, Dapper, PostgreSQL (Neon) via Npgsql
- Frontend: React, TypeScript, Vite, Tailwind CSS, React Router, TanStack Query
- Monorepositório: o Vite faz proxy de `/api` para a API em `http://localhost:5080`

## Como rodar

Pré-requisitos: .NET 8 SDK, Node.js 22 e um banco PostgreSQL.

1. Configure a connection string fora do Git (user-secrets):

```powershell
cd backend
dotnet user-secrets set "ConnectionStrings:Default" "Host=SEU_HOST;Database=neondb;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=Require;Channel Binding=Require" --project src/NexusFinance.Api
```

2. Suba a API. Na primeira execução ela cria o schema `nexus` e aplica as migrations:

```powershell
dotnet run --project src/NexusFinance.Api
```

A API fica em `http://localhost:5080`. Confira `http://localhost:5080/health`.

3. Em outro terminal, suba o frontend:

```powershell
cd frontend
npm install
npm run dev
```

Abra `http://localhost:5173`. A primeira tela é o login. Usuário demo: `demo@nexusfinance.dev` / `Demo@123`. O seed entra sozinho se ainda não houver clientes no schema `nexus`.

## Testes

```powershell
cd backend
dotnet test
```

## Estrutura

```
/backend        Api, Application, Domain, Infrastructure e testes
/frontend       React
/database       migrations SQL (001_..., 002_...)
/docs           backlog do MVP
```

Tabelas e colunas em português. Código C# e TypeScript em inglês.

## Deploy no Render

A imagem sobe o frontend e a API no mesmo serviço. O React publicado chama `/api` no mesmo domínio.

1. No [Render](https://render.com), crie um **Blueprint** apontando para este repositório (ele lê o `render.yaml`) ou um **Web Service** com runtime Docker, Dockerfile `./Dockerfile` e contexto na raiz.
2. Na variável `ConnectionStrings__Default`, cole a connection string do Neon:

```text
Host=SEU_HOST;Database=neondb;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=Require;Channel Binding=Require
```

3. No primeiro start a API cria o schema `nexus` e aplica as migrations. O health check do Render é `GET /health` e só fica verde com o banco acessível.
4. O Render define `PORT`. A API escuta `0.0.0.0` nessa porta.

Para testar a imagem na máquina:

```powershell
$env:NEXUS_CONNECTION_STRING = "Host=SEU_HOST;Database=neondb;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=Require;Channel Binding=Require"
docker compose up --build
```

Abra `http://localhost:8080`.
