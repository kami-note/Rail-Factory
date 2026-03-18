# Matriz de Requisitos Detalhada por Microserviço - RAIL Factory

Este documento apresenta a lista expandida de requisitos funcionais e não funcionais para o ecossistema RAIL Factory.

---

## 1. Identity & Access Management (IAM) Service
Responsável pela segurança, governança e isolamento multi-camada.

| ID | Requisito | Descrição | Prioridade |
|:---|:---|:---|:---|
| **RF-IAM-01** | Autenticação SSO Google | Login único via OAuth2 utilizando contas corporativas Google Workspace. | Essencial |
| **RF-IAM-02** | Provisionamento de Tenants | Cadastro de novas unidades (Matriz/Filiais) com CNPJ, endereço e configurações regionais. | Essencial |
| **RF-IAM-03** | RBAC Granular | Definição de papéis (Roles) e permissões (Claims) por recurso (Ex: `production.write`, `inventory.view`). | Essencial |
| **RF-IAM-04** | Gestão de Sessões | Controle de sessões ativas, revogação de tokens e timeout de inatividade. | Importante |
| **RF-IAM-05** | Trilhas de Auditoria (Audit Log) | Registro imutável de quem, quando, onde (IP) e o que foi alterado em qualquer microserviço. | Essencial |
| **RF-IAM-06** | API Key Management | Geração e revogação de chaves de API para integrações externas (B2B/SEFAZ). | Importante |
| **RF-IAM-07** | Recuperação de Conta | Fluxo de recuperação de senha e MFA (Multi-Factor Authentication). | Importante |

---

## 2. Production Service (Manufatura)
O motor do sistema, responsável pela transformação de matéria-prima em produto.

| ID | Requisito | Descrição | Prioridade |
|:---|:---|:---|:---|
| **RF-PRD-01** | Versionamento de BOM | Cadastro de Bill of Materials com controle de versão e data de vigência. | Essencial |
| **RF-PRD-02** | Gestão de Work Centers | Cadastro de centros de trabalho (máquinas, linhas de montagem, bancadas manuais). | Essencial |
| **RF-PRD-03** | Ciclo de Vida da OP | Estados da Ordem de Produção: Rascunho, Planejada, Liberada, Em Execução, Pausada, Finalizada, Cancelada. | Essencial |
| **RF-PRD-04** | Reserva de Materiais | Bloqueio automático de insumos no estoque assim que uma OP é liberada. | Essencial |
| **RF-PRD-05** | Registro de Refugo (Scrap) | Lançamento de perdas de materiais durante o processo com justificativa técnica. | Importante |
| **RF-PRD-06** | Apontamento de Parada | Registro de interrupções nas máquinas (manutenção, falta de material, falta de energia). | Importante |
| **RF-PRD-07** | Controle de Qualidade | Etapas de inspeção obrigatórias antes da finalização de uma OP. | Importante |
| **RF-PRD-08** | Lote e Rastreabilidade | Geração de números de lote para produtos acabados vinculados aos lotes de insumos. | Essencial |

---

## 3. Supply Chain Service (Inbound)
Focado na aquisição, recebimento e homologação de fornecedores.

| ID | Requisito | Descrição | Prioridade |
|:---|:---|:---|:---|
| **RF-SUP-01** | Monitoramento de XML (SEFAZ) | Captura automática de NF-e emitidas contra o CNPJ da empresa via Manifestação do Destinatário. | Essencial |
| **RF-SUP-02** | Conferência Cega | Processo de conferência de materiais recebidos sem visualizar as quantidades da nota original. | Importante |
| **RF-SUP-03** | Cadastro de Fornecedores | Workflow de homologação de fornecedores (certidões, capacidade técnica). | Importante |
| **RF-SUP-04** | Pedidos de Compra (PO) | Emissão e acompanhamento de pedidos de compra enviados aos fornecedores. | Essencial |
| **RF-SUP-05** | Rating de Fornecedor | Avaliação automática de fornecedores baseada em prazo de entrega e qualidade dos materiais. | Desejável |
| **RF-SUP-06** | Gestão de Devoluções | Fluxo de logística reversa para materiais com defeito ou divergentes. | Importante |

---

## 4. Logistics Service (Outbound)
Responsável pela saída estratégica de produtos e satisfação do cliente.

| ID | Requisito | Descrição | Prioridade |
|:---|:---|:---|:---|
| **RF-LOG-01** | Picking & Packing | Processo guiado de separação de itens e embalagem para expedição. | Essencial |
| **RF-LOG-02** | Gestão de Transportadoras | Cadastro de parceiros logísticos, tabelas de frete e prazos por região. | Importante |
| **RF-LOG-03** | Rastreio de Entrega (Tracking) | Portal para o cliente acompanhar o status da entrega em tempo real. | Importante |
| **RF-LOG-04** | Webhooks de Status | Notificação automática (Push/Email) para sistemas externos sobre mudanças no despacho. | Desejável |
| **RF-LOG-05** | Conferência de Embarque | Validação final de que os volumes carregados no veículo conferem com a nota fiscal. | Essencial |
| **RF-LOG-06** | Cálculo de Frete | Motor de cálculo de frete baseado em peso, cubagem e distância. | Importante |

---

## 5. Fleet Service (Gestão de Frota)
Controle total sobre os ativos de transporte.

| ID | Requisito | Descrição | Prioridade |
|:---|:---|:---|:---|
| **RF-FLE-01** | Prontuário do Veículo | Registro completo: Chassi, Placa, Renavam, Documentação (CRLV) e Vencimentos. | Essencial |
| **RF-FLE-02** | Plano de Manutenção | Agenda de manutenções preventivas (Troca de óleo, pneus, revisões) por KM ou tempo. | Importante |
| **RF-FLE-03** | Controle de Abastecimento | Registro de consumo de combustível vinculado a motoristas e rotas. | Importante |
| **RF-FLE-04** | Alocação de Motoristas | Gestão de escalas de trabalho e vínculos motorista/veículo. | Essencial |
| **RF-FLE-05** | Roteirização Inteligente | Otimização de múltiplas paradas para reduzir custo de combustível e tempo. | Desejável |
| **RF-FLE-06** | Telemetria Básica | Registro de ocorrências (excesso de velocidade, paradas não programadas). | Desejável |

---

## 6. HR Service (Cadastro de Pessoas / Dados de Terceiros)
Mantém **dados de outras pessoas** que precisam ser referenciadas em documentos e por outros serviços (ex.: motorista em documentos de entrega, colaborador em ordens de produção). Essas pessoas **podem não ter acesso ao sistema** — estão no sistema apenas como cadastro (master data).

| ID | Requisito | Descrição | Prioridade |
|:---|:---|:---|:---|
| **RF-HRS-01** | Cadastro de Pessoas | Dados de colaboradores, motoristas e demais pessoas referenciadas em documentos (dados pessoais, cargo, histórico). A pessoa pode estar no sistema sem possuir acesso a ele. | Essencial |
| **RF-HRS-02** | Matriz de Competências | Registro de habilidades técnicas por pessoa (ex: operador de solda, empilhadeira). | Importante |
| **RF-HRS-03** | Escalas e Turnos | Definição de jornadas de trabalho por pessoa ou função (6x1, 5x2, turnos noturnos). | Essencial |

---

## 7. Dashboard & Reporting Service
Inteligência de negócio e monitoramento operacional.

| ID | Requisito | Descrição | Prioridade |
|:---|:---|:---|:---|
| **RF-DSH-01** | OEE (Overall Equipment Effectiveness) | Cálculo automático da eficácia global das máquinas da fábrica. | Essencial |
| **RF-DSH-02** | Mapas de Calor de Entrega | Visualização geográfica das entregas em andamento e atrasos. | Desejável |
| **RF-DSH-03** | Alertas em Tempo Real | Notificações de falha de máquina ou estoque crítico. | Essencial |
| **RF-DSH-04** | Exportação Multiformato | Geração de relatórios em PDF, Excel e CSV. | Importante |
| **RF-DSH-05** | Dashboard de Custos | Visão financeira de custo de produção vs. preço de venda. | Importante |

---

## 8. Requisitos Não Funcionais (Detalhados)

| ID | Requisito | Descrição |
|:---|:---|:---|
| **RNF-01** | Resiliência (Circuit Breaker) | Uso de padrões como Circuit Breaker e Retry (Polly) para lidar com falhas de serviços. |
| **RNF-02** | Consistência Eventual | Garantir entrega de mensagens via Outbox Pattern para evitar perda de dados em falhas do RabbitMQ. |
| **RNF-03** | Observabilidade | Centralização de logs (Serilog), métricas (Prometheus) e rastreamento (OpenTelemetry). |
| **RNF-04** | Performance de API | APIs de consulta devem responder em < 500ms (P95). |
| **RNF-05** | Segurança de Dados | Criptografia em repouso (AES-256) para dados sensíveis e TLS 1.3 em trânsito. |
| **RNF-06** | Escalabilidade Horizontal | Todos os serviços devem suportar múltiplas instâncias atrás de um Load Balancer. |
| **RNF-07** | Localização | Suporte a múltiplos idiomas (i18n) e fusos horários por filial. |
