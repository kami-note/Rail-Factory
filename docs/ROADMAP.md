# 📋 Kanban Roadmap Completo - RAIL Factory

Este documento detalha o ciclo de vida completo do projeto, desde a concepção teórica até o deploy final.

---

## ⚪ FASE 0: DESIGN E MODELAGEM (DOCUMENTAÇÃO)
*Foco: Atender aos requisitos acadêmicos e definir a arquitetura.*

- [ ] **[DOC]** Elaborar Diagrama de Casos de Uso (Geral e por Microserviço).
- [ ] **[DOC]** Modelar Processos de Negócio em **BPMN** (Produção e Inbound).
- [ ] **[DOC]** Criar Diagrama de Classes e Diagrama de Entidade-Relacionamento (DER).
- [ ] **[DOC]** Desenhar Diagrama de Sequência para fluxos críticos (Ex: Reserva de Estoque).
- [ ] **[DOC]** Elaborar Diagrama de Arquitetura e Componentes (Visão Aspire/Docker).
- [ ] **[UI/UX]** Criar Wireframes/Mockups das telas principais no Figma/Penpot.

---

## 🟦 FASE 1: INFRAESTRUTURA E SEGURANÇA (BACKLOG)
*Foco: Alicerce técnico e isolamento de dados.*

- [ ] **[INFRA]** Configurar Orquestração .NET Aspire (Postgres, RabbitMQ, Redis).
- [ ] **[IAM]** Implementar Multitenancy "Database per Tenant" ou "Schema per Tenant".
- [ ] **[IAM]** RF-IAM-01: Autenticação SSO Google (OAuth2).
- [ ] **[IAM]** RF-IAM-03: RBAC Granular e Gestão de Claims.
- [ ] **[CORE]** Criar Shared Kernel (Exceções comuns, Tipos Base, Eventos de Domínio).
- [ ] **[WEB]** Setup do Projeto Blazor WebApp com Layout Base e Temas.

---

## 🟧 FASE 2: MANUFATURA E CORE OPERACIONAL
*Foco: O coração operacional da fábrica.*

- [ ] **[PRD]** Implementar Entidades de Domínio (BOM, WorkCenter, ProductionOrder).
- [ ] **[PRD]** RF-PRD-01: Versionamento de BOM e Engenharia de Produto.
- [ ] **[PRD]** RF-PRD-03: Workflow de Estados da OP via MassTransit (State Machine).
- [ ] **[PRD]** RF-PRD-04: Lógica de Reserva e Baixa de Materiais (Consumo).
- [ ] **[WEB]** CRUDs de Produção e Cockpit do Operador em Blazor.

---

## 🟩 FASE 3: SUPPLY CHAIN E LOGÍSTICA EXTERNA
*Foco: Entrada, saída e gestão de ativos móveis.*

- [ ] **[SUP]** RF-SUP-01: Cliente de Integração SEFAZ e Parser de XML.
- [ ] **[SUP]** RF-SUP-02: Tela de Recebimento de Materiais e Conferência.
- [ ] **[LOG]** RF-LOG-01: Módulo de Expedição (Separação e Carga).
- [ ] **[FLE]** RF-FLE-01: Gestão de Frota e Controle de Manutenções.
- [ ] **[LOG]** RF-LOG-04: Dispatch de Webhooks para Clientes (Event-Driven).

---

## 🟪 FASE 4: RH E INTELIGÊNCIA DE DADOS (BI)
*Foco: Performance humana e eficiência de máquinas.*

- [ ] **[HRS]** RF-HRS-01/04: Registro de Ponto e Apontamento de Mão de Obra.
- [ ] **[HRS]** RF-HRS-02: Validação de Competências para Operação de Máquinas.
- [ ] **[DSH]** RF-DSH-01: Engine de Cálculo de OEE (Disponibilidade x Performance x Qualidade).
- [ ] **[DSH]** Dashboards em Real-time usando SignalR (Gráficos e Alertas).

---

## 🟥 FASE 5: QUALIDADE, TESTES E FINALIZAÇÃO
*Foco: Robustez e entrega final.*

- [ ] **[TEST]** Implementar Testes Unitários e de Integração (xUnit/FluentAssertions).
- [ ] **[TECH]** Aplicar Outbox Pattern em todos os serviços com mensageria.
- [ ] **[TECH]** Configurar Health Checks e Observabilidade (Grafana/Seq).
- [ ] **[DOC]** Redigir o Relatório Técnico Final (Release Notes) e Manual do Usuário.
- [ ] **[DEPLOY]** Preparar configuração de deploy baseada no Aspire AppHost para ambiente de Homologação.

---

## 🏷️ Legenda de Status
- ⬜ **Backlog:** Aguardando início.
- 🚧 **In Progress:** Em desenvolvimento.
- ✅ **Done:** Validado e finalizado.
