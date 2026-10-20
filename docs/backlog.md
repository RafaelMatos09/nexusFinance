# Nexus Finance — Backlog do MVP

Plataforma fictícia de gestão financeira e cobrança para PMEs. Projeto de portfólio (GitHub, LinkedIn, Workana).
Escopo deste backlog: **somente o MVP** (Fase 1). Roles, auditoria, Docker/Azure e IA ficam para depois.

Decisões fechadas no E0:

- Banco: PostgreSQL (Neon) com Npgsql e Dapper. O database `neondb` já existe; as migrations não criam o database.
- Dev: Vite faz proxy de `/api` para `http://localhost:5080`.
- Tabelas e colunas em português. Código C# e TypeScript em inglês.
- Connection string somente em user-secrets ou na variável `ConnectionStrings__Default` do Render, nunca no Git.
- Deploy: `Dockerfile` e `render.yaml` na raiz. A imagem publica o React e a API no mesmo serviço.

## Contexto para o Cursor

- Backend: C# .NET 8, Web API, Dapper, PostgreSQL, arquitetura em camadas (Api, Application, Domain, Infrastructure).
- Frontend: React + TypeScript, Vite, Tailwind CSS, Formik + Yup, Radix UI, React Router, TanStack Query.
- Todos os dados são fictícios. Nunca usar dados ou código de sistemas reais.
- Regras: DTOs separados das entidades; validação no backend e no frontend; erros padronizados (`ProblemDetails`); nomes de tabelas e colunas em português; código em inglês; commits pequenos (Conventional Commits).
- Ao implementar cada tarefa: criar a migration SQL, o repositório, o endpoint, a tela e um teste mínimo.

## Ordem de execução sugerida

| Bloco | Épicos | Tempo estimado |
|---|---|---|
| Fim de semana 1 | E0 Setup, E1 Acesso | 6–8 h |
| Fim de semana 2 | E2 Clientes, E3 Contratos | 8–10 h |
| Fim de semana 3 | E4 Cobranças, E5 Recebimentos | 8–10 h |
| Fim de semana 4 | E6 Dashboard, E7 Relatório, E8 Entrega | 8–10 h |

---

## E0 — Setup do projeto

- [x] **0.1** Criar repositório com pastas `/backend`, `/frontend`, `/database`, `/docs`.
- [x] **0.2** Solution .NET com projetos Api, Application, Domain, Infrastructure.
- [x] **0.3** Conexão com PostgreSQL via Dapper (`IDbConnectionFactory`) e script inicial em `/database/migrations`.
- [x] **0.4** Frontend com Vite + TS + Tailwind + Router + TanStack Query; layout base (sidebar, header, área de conteúdo). Proxy de `/api` para a API.
- [x] **0.5** Tratamento global de erros na API e CORS configurado.
- [x] **0.6** `README.md` inicial com como rodar o projeto.

**Pronto quando:** API responde `/health` e o frontend abre com o layout base.

## E1 — Acesso (simples)

- [ ] **1.1** Tabela `Usuarios` (id, nome, email, senha_hash, ativo).
- [ ] **1.2** `POST /auth/login` com JWT.
- [ ] **1.3** Tela de login com Formik + Yup.
- [ ] **1.4** Rota protegida no frontend e guarda de token.
- [ ] **1.5** Seed com um usuário demo.

**Pronto quando:** só é possível acessar as telas após login; usuário demo funciona.

## E2 — Clientes

- [ ] **2.1** Tabela `Clientes` (id, nome/razão social, documento, email, telefone, status, criado_em).
- [ ] **2.2** Endpoints CRUD com paginação, busca por nome/documento e filtro por status.
- [ ] **2.3** Validação de CPF/CNPJ (backend e frontend).
- [ ] **2.4** Tela de listagem com tabela, filtros e paginação.
- [ ] **2.5** Formulário de criar/editar em modal ou página (Formik + Radix).
- [ ] **2.6** Inativar cliente em vez de excluir.

**Pronto quando:** CRUD completo funcionando, sem duplicar documento.

## E3 — Contratos

- [ ] **3.1** Tabela `Contratos` (id, cliente_id, descrição, valor, periodicidade, data_inicio, data_fim, status).
- [ ] **3.2** CRUD de contratos vinculado ao cliente.
- [ ] **3.3** Status: Rascunho, Ativo, Encerrado, Cancelado, com regras de transição.
- [ ] **3.4** Tela de contratos com filtro por cliente e status.
- [ ] **3.5** Aba "Contratos" dentro do detalhe do cliente.

**Pronto quando:** contrato ativo gera base para cobranças e transições inválidas são bloqueadas.

## E4 — Cobranças

- [ ] **4.1** Tabela `Cobrancas` (id, contrato_id, cliente_id, valor, vencimento, status, observação).
- [ ] **4.2** Geração de cobranças a partir do contrato (mensal, trimestral, anual).
- [ ] **4.3** Status: Pendente, Paga, Vencida, Cancelada; job simples (ou cálculo na consulta) que marca vencidas.
- [ ] **4.4** Cálculo de multa e juros por atraso (percentuais configuráveis).
- [ ] **4.5** Tela de cobranças com filtros por período, cliente e status.
- [ ] **4.6** Ação "Cancelar cobrança" com confirmação.

**Pronto quando:** cobranças aparecem corretamente e o valor atualizado com multa e juros bate com o esperado em testes.

## E5 — Recebimentos

- [ ] **5.1** Tabela `Recebimentos` (id, cobranca_id, valor_pago, data_pagamento, forma_pagamento).
- [ ] **5.2** Registrar baixa total ou parcial; atualizar status da cobrança.
- [ ] **5.3** Impedir pagamento maior que o saldo devido.
- [ ] **5.4** Tela/modal de baixa dentro da lista de cobranças.
- [ ] **5.5** Histórico de recebimentos por cobrança.

**Pronto quando:** baixa parcial mantém a cobrança em aberto com saldo correto e baixa total a marca como paga.

## E6 — Dashboard

- [ ] **6.1** Endpoint de indicadores: total a receber, recebido no mês, vencido, taxa de inadimplência.
- [ ] **6.2** Gráfico de recebimentos por mês (últimos 6 meses).
- [ ] **6.3** Aging de cobranças (0–30, 31–60, 61–90, 90+ dias).
- [ ] **6.4** Top 5 clientes em atraso.
- [ ] **6.5** Cards e gráficos no frontend com estado de carregamento e vazio.

**Pronto quando:** números do dashboard conferem com os dados do seed.

## E7 — Relatório com exportação

- [ ] **7.1** Relatório de cobranças com filtros (período, cliente, status).
- [ ] **7.2** Exportação para Excel com abas **DASHBOARD** (resumo) e **DADOS** (linhas).
- [ ] **7.3** Botão "Exportar" no frontend com feedback de download.

**Pronto quando:** o arquivo Excel baixa com as duas abas e totais corretos.

## E8 — Entrega e portfólio

- [ ] **8.1** Script de seed com dados fictícios realistas (30 clientes, contratos e cobranças variadas).
- [ ] **8.2** Testes de unidade para regras críticas (multa/juros, transições de status, baixa parcial).
- [ ] **8.3** README completo: visão geral, stack, como rodar, prints e gif das telas principais.
- [ ] **8.4** Deploy da demo online (frontend e API) com usuário demo no README.
- [ ] **8.5** Publicar no GitHub e descrever o projeto no perfil do LinkedIn e do Workana.

**Pronto quando:** qualquer pessoa consegue abrir a demo ou rodar o projeto seguindo o README.

---

## Fora do MVP (fases futuras)

Perfis e permissões, auditoria, filtros avançados, Azure, CI/CD, notificações de vencimento, jobs agendados e IA.
