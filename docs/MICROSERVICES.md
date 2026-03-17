# Documentação de Microserviços - RAIL Factory

Este documento descreve a arquitetura de microserviços planejada para o sistema **RAIL Factory**, mapeando as responsabilidades e requisitos funcionais de cada serviço com base no Documento de Definição de Escopo (DDE).

## 1. Identity & Access Management (IAM) Service
**Responsabilidade:** Gerenciar identidades, autenticação, autorização e multitenancy.
**Tecnologias:** IdentityServer / Keycloak (ou implementação customizada com .NET 8 Identity), OAuth2 (Google).

### Requisitos Funcionais
*   **Autenticação:** Login via OAuth2 (Google).
*   **Gestão de Tenants:** Criação e gerenciamento de unidades fabris (matriz e filiais).
*   **Controle de Acesso (RBAC):** Gerenciamento de perfis de usuário:
    *   *Admin Matriz:* Visão global.
    *   *Admin Filial:* Visão restrita à unidade.
    *   *Operador:* Acesso operacional restrito.
*   **Auditoria:** Registro de logs de alterações críticas (quem, quando, o quê).

---

## 2. Production Service (Manufatura)
**Responsabilidade:** Core do chão de fábrica. Gerencia o ciclo de vida da produção e estoques locais.
**Isolamento:** Banco de dados dedicado por tenant (schema isolation).

### Requisitos Funcionais
*   **Gestão de BOM (Bill of Materials):** Cadastro de listas de materiais para produtos.
*   **Ordens de Produção (OP):** Criação, agendamento e acompanhamento de OPs.
*   **Controle de Etapas:** Registro de início e fim de etapas de produção.
*   **Estoque Local:** Movimentação interna de insumos e produtos acabados dentro da fábrica.

---

## 3. Supply Chain Service (Inbound)
**Responsabilidade:** Gestão de suprimentos e entrada de materiais.

### Requisitos Funcionais
*   **Entrada de Notas Fiscais:** Importação automática via API da SEFAZ (leitura de XML/Chave de Acesso).
*   **Entrada Manual:** Interface de contingência para registro manual de entrada de materiais.
*   **Gestão de Fornecedores:** Cadastro e histórico de fornecimentos.
*   **Atualização de Estoque:** Disparo de eventos para atualizar o estoque no *Production Service*.

---

## 4. Logistics Service (Outbound)
**Responsabilidade:** Gestão de expedição e logística de saída.

### Requisitos Funcionais
*   **Registro de Despacho:** Associação de produtos acabados a pedidos de saída.
*   **Integração B2B:** Disparo de webhooks para clientes notificando status (Em separação, Despachado, Entregue).
*   **Rastreabilidade:** Histórico de saída de lotes.

---

## 5. Fleet Service (Gestão de Frota)
**Responsabilidade:** Controle da frota própria e terceirizada.

### Requisitos Funcionais
*   **Cadastro de Veículos:** Registro de características (capacidade, tipo de carga).
*   **Gestão de Rotas:** Sugestão de rotas baseadas nas entregas (integração com mapas/APIs de roteirização).
*   **Manutenção:** (Futuro) Controle de manutenção preventiva.

---

## 6. HR Service (Recursos Humanos)
**Responsabilidade:** Gestão da força de trabalho no chão de fábrica.

### Requisitos Funcionais
*   **Apontamento de Horas:** Registro de horas trabalhadas por operador em cada OP.
*   **Integração Contábil:** Exportação de dados para softwares de folha de pagamento/contabilidade.

---

## 7. Dashboard & Reporting Service
**Responsabilidade:** Agregação de dados para visualização gerencial (BFF - Backend for Frontend ou Serviço de Agregação).

### Requisitos Funcionais
*   **Indicadores (KPIs):**
    *   Eficiência de produção.
    *   Níveis de estoque (Matéria-prima vs. Produto Acabado).
    *   Status de entregas em tempo real.
*   **Visão Consolidada:** Painéis para a Matriz com dados agregados de todas as filiais.
