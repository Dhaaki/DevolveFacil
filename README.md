# DevolveFacil

Plataforma de gestão de devoluções e trocas da La Moda. Permite que clientes solicitem devoluções via autoatendimento e oferece à equipe de operações visibilidade completa do ciclo de vida — desde a geração de etiqueta até a avaliação de qualidade, emissão de vale ou reembolso e notificação ao ERP.

## Stack

| Camada | Tecnologia |
|---|---|
| Backend | ASP.NET Core 8 (C# 12) |
| Banco de dados | PostgreSQL 15 + Entity Framework Core 8 |
| Fila de mensagens | MassTransit + RabbitMQ |
| Armazenamento de arquivos | MinIO (S3-compatible) |
| Frontend | React 18 + Vite + TailwindCSS |
| Containerização | Docker + Docker Compose |

## Arquitetura

Monolito modular com arquitetura Hexagonal (Ports & Adapters). O domínio (`Core`) não possui dependências externas — integrações com VTEX, Correios e SAP são implementadas como adapters intercambiáveis, selecionados por configuração.

```
DevolveFacill/
├── Src/
│   ├── DevolveFacill.Core/           # Domínio + casos de uso
│   ├── DevolveFacill.Api/            # Controllers HTTP + autenticação JWT
│   ├── DevolveFacill.Infrastructure/ # EF Core + repositórios
│   ├── DevolveFacill.Adapters/       # VTEX, Correios, SAP, MinIO
│   └── DevolveFacill.Workers/        # Consumers MassTransit (background)
└── Frontend/                         # React SPA
```

## Rodando localmente

**Pré-requisitos:** Docker Desktop instalado.

```bash
# 1. Copie e configure as variáveis de ambiente
cp .env.example .env
# edite o .env com suas credenciais

# 2. Suba todos os serviços
docker compose up -d

# 3. Acesse
# Frontend:  http://localhost:3000
# API:       http://localhost:8080
# RabbitMQ:  http://localhost:15672  (guest/guest)
# MinIO:     http://localhost:9001   (minioadmin/minioadmin)
```

O banco de dados é criado automaticamente na primeira execução. Um usuário admin padrão é criado com as credenciais definidas em `SEED_ADMIN_EMAIL` e `SEED_ADMIN_PASSWORD` no `.env`.

## Desenvolvimento

O projeto usa **GitFlow**:

```
main       ← produção (só via PR de develop)
develop    ← integração (só via PR de feature/*)
feature/*  ← desenvolvimento de funcionalidades
hotfix/*   ← correções urgentes em produção
```

Após alterar o código do backend, reconstrua a imagem:

```bash
docker compose up --build api -d
```

## Fluxo de uma devolução

```
Draft → PendingLabel → LabelGenerated → InTransit → Delivered
                                                         ↓
                                              QualityApproved / QualityRejected
                                                         ↓
                                     PendingRefund / PendingVoucher / ClosedRejected
                                                         ↓
                                          ClosedRefund / ClosedExchange
```

Transitions inválidas são bloqueadas pelo `ReturnRequestStateMachine`. Cada transição gera um `ReturnEvent` imutável — trilha de auditoria completa.

## Integrações

| Serviço | Adapter real | Status |
|---|---|---|
| VTEX (pedidos + vales) | `VtexCommercePlatform` | Fase 4 |
| Correios (etiquetas + rastreamento) | `CorreiosCarrier` | Fase 4 |
| SAP S/4HANA (ERP) | `SapErpIntegration` | Fase 4 |
| MinIO / AWS S3 (arquivos) | `S3StorageService` | Implementado |

Em desenvolvimento, todas as integrações usam adapters `Mock` configurados via `.env`.

## Variáveis de ambiente

Veja `.env.example` para a lista completa. As principais:

| Variável | Descrição |
|---|---|
| `POSTGRES_PASSWORD` | Senha do banco de dados |
| `JWT_KEY` | Chave secreta para assinar tokens (mín. 32 chars) |
| `COMMERCE_PROVIDER` | `Mock` ou `Vtex` |
| `CARRIER_PROVIDER` | `Mock` ou `Correios` |
| `ERP_PROVIDER` | `Mock` ou `Sap` |
| `STORAGE_PROVIDER` | `Mock` ou `S3` |
