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

Monolito modular com arquitetura Hexagonal (Ports & Adapters). O domínio (`Core`) não possui dependências externas — integrações com VTEX, Correios e SAP são implementadas como adapters intercambiáveis, selecionados por configuração. Os consumers de filas e o processamento assíncrono ficam em `Workers` (MassTransit + RabbitMQ).

## Rodando localmente

**Pré-requisitos:** Docker Desktop instalado.

```bash
# 1. Crie o arquivo de variáveis de ambiente
cp .env.example .env
# Preencha POSTGRES_PASSWORD, JWT_KEY e SEED_ADMIN_PASSWORD no .env

# 2. Suba todos os serviços
docker compose up -d

# 3. Após alterar código do backend, reconstrua
docker compose up --build api -d

# Interfaces disponíveis
# Frontend:  http://localhost:3000
# API:       http://localhost:8080
# RabbitMQ:  http://localhost:15672
# MinIO:     http://localhost:9001
```

O banco de dados é criado automaticamente na primeira execução. Um usuário admin padrão é criado com as credenciais definidas em `SEED_ADMIN_EMAIL` e `SEED_ADMIN_PASSWORD` no `.env`.

## Desenvolvimento

Branches seguem GitFlow: `feature/*` → `develop` → `main`, somente via PR.

## Fluxo de uma devolução

```
Draft → PendingLabel → LabelGenerated → InTransit → Delivered
                                                         ↓
                                              QualityApproved / QualityRejected
                                                         ↓
                                     PendingRefund / PendingVoucher / ClosedRejected
                                                         ↓
                                          ClosedRefund / ClosedExchange

Qualquer estado → Cancelled (cancelamento pelo cliente ou admin)
Qualquer estado → Error     (falha que exige intervenção manual)
```

Transições inválidas são bloqueadas pelo `ReturnRequestStateMachine`. Cada transição gera um `ReturnEvent` imutável — trilha de auditoria completa.

## Integrações

| Serviço | Adapter real | Status |
|---|---|---|
| VTEX (pedidos + vales) | `VtexCommercePlatform` | Fase 4 |
| Correios (etiquetas + rastreamento) | `CorreiosCarrier` | Fase 4 |
| SAP S/4HANA (ERP) | `SapErpIntegration` | Fase 4 |
| MinIO / AWS S3 (arquivos) | `S3StorageService` | Implementado |

Em desenvolvimento, todas as integrações usam adapters `Mock` configurados via `COMMERCE_PROVIDER`, `CARRIER_PROVIDER`, `ERP_PROVIDER` e `STORAGE_PROVIDER` no `.env`.
