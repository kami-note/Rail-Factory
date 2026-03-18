# Use Case Diagrams (PlantUML)

Diagramas de casos de uso extraídos de [phase0/USE-CASE-DIAGRAM.md](../phase0/USE-CASE-DIAGRAM.md).

## Versionamento

Os arquivos seguem o sufixo **-v1** (versão 1). Novas versões devem usar **-v2**, **-v3**, etc., mantendo as anteriores para histórico.

| Arquivo | Descrição |
|---------|-----------|
| `geral-use-case-v1.puml` | Diagrama geral — RAIL Factory com todos os pacotes (IAM, Production, Supply, Logistics, Fleet, HR, Dashboard). |
| `iam-use-case-v1.puml` | IAM (Identity & Access Management). |
| `production-use-case-v1.puml` | Production (Manufatura). |
| `supply-chain-use-case-v1.puml` | Supply Chain (Inbound). |
| `logistics-use-case-v1.puml` | Logistics (Outbound). |
| `fleet-use-case-v1.puml` | Fleet (Gestão de Frota). |
| `hr-use-case-v1.puml` | HR (Cadastro de Pessoas). |
| `dashboard-use-case-v1.puml` | Dashboard & Reporting. |

*O arquivo `geral_use_case_diagram_v1.puml` é uma versão anterior do diagrama geral; o conteúdo canônico está em `geral-use-case-v1.puml`.*

**Como usar:** Abra os `.puml` no VS Code/Cursor com a extensão [PlantUML](https://marketplace.visualstudio.com/items?itemName=jebbs.plantuml) para pré-visualizar e exportar PNG/SVG.

## Gerar imagens automaticamente

Na raiz do repositório:

```bash
./scripts/render-plantuml-diagrams.sh
```

- **Saída**: `docs/diagram/rendered/*.png`
- **SVG**: `FORMAT=svg ./scripts/render-plantuml-diagrams.sh`
- **Filtrar versão**: `VERSION_FILTER=v1 ./scripts/render-plantuml-diagrams.sh`
