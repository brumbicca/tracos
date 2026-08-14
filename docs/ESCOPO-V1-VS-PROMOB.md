# Traços 3D Studio — Escopo V1 vs Promob

**Marco:** V1 feature-complete · **V2 encerrado**  
**Data:** 01/07/2026 (consolidação documentação)  
**Build de referência:** `2026.06.26.2012` (`dist/last-build.txt`)  
**Testes:** **398** (`Tracos3DStudio.Tests`)

Documento de **encerramento dos ciclos V1 e V2**: o que foi entregue, validações pendentes opcionais e ponte para a **trilha V3**.

**Trilha ativa (a partir de 01/07/2026):** [ESCOPO-V3-PROMOB-COMPLETO.md](./ESCOPO-V3-PROMOB-COMPLETO.md)  
**Plano operacional:** [PLANO-EXECUCAO.md](./PLANO-EXECUCAO.md) · **Manual:** [manual/README.md](./manual/README.md)

---

## O que é o V1

Desktop **Windows offline**, arquivo **`.tracos`**, fluxo ponta a ponta:

```
Ambiente → módulos → orçamento/PDF → detalhamento → plano de corte → instalador
```

Referência de mercado: [Promob Plus](https://suporte.promob.com/hc/pt-br/articles/31123224474257-Plus) — **paridade prática**, não clone integral *(decisão revisada em V3 — ver documento V3)*.

---

## Entregue no V1 (✅)

| Bloco | Conteúdo | Manual / paridade |
|-------|----------|-------------------|
| **Fases 1–5** | Ambiente, aberturas, módulos, orçamento, técnico, produção básica | Manual por seção |
| **A** | Camadas, faixas, regiões (parede + piso), cauda A.6, R8–R9 | [07-promob-paridade camadas](./manual/paredes/07-promob-paridade-camadas-faixas-regioes.md) |
| **B.2** | Abas Inserir/Ambiente, busca, drag L6, lista ambiente A7/A9/A10 | [07-promob-paridade biblioteca](./manual/modulos/07-promob-paridade-biblioteca.md) |
| **C** | Janela Materiais, drag, modo, copiar, face livre | [07-promob-paridade materiais](./manual/materiais/07-promob-paridade-materiais.md) |
| **D** | Validade, desconto, pagamento, vendedor, obs. comerciais (PDF) | [orcamento/](./manual/orcamento/README.md) |
| **E.1–E.3** | JSON máquina, CSV furos CNC, JSON `tracos-cnc-job` | [producao/](./manual/producao/README.md) |
| **Escala** | Instalador self-contained, biblioteca, backup, ERP JSON | [escala/](./manual/escala/README.md) |

**Fixture principal:** `fase-2-cozinha-L.tracos` (cozinha L, 4 módulos).

---

## Entregue no V2 (✅) — backlog pós-V1 fechado

| Release | ID | Entrega | Data |
|---------|-----|---------|------|
| V2.1 | **L7** | Biblioteca Painéis | 26/06/2026 |
| V2.2 | **A5** | Renomear instância na lista Ambiente | 26/06/2026 |
| V2.3 | **A3** | Lista agrupada por parede | 26/06/2026 |
| V2.4 | **A8** | Multi-seleção na lista Ambiente | 26/06/2026 |
| V2.5 | **A4** | Visível / Bloqueado na aba Ambiente | 26/06/2026 |
| V2.6 | **L10** | Recarregar biblioteca sem reiniciar | 26/06/2026 |
| V2.7 | **A3c** | Lista por cômodo → parede + **Projeto → Adicionar cômodo** | 26/06/2026 |
| V2.8 | **E.4** | Export `.tap` Jaraguá Mach4 (Solid TAF) | 26/06/2026 |

Detalhes e gates: [PLANO-EXECUCAO.md](./PLANO-EXECUCAO.md) · [ESCOPO-V3 §2.3](./ESCOPO-V3-PROMOB-COMPLETO.md#23-releases-v2-pós-marco-v1)

---

## Parcial / validação (🟡) — não bloqueia marcos V1/V2

| Item | Traços hoje | Nota |
|------|-------------|------|
| Smoke instalador Parte A | VM limpa pendente **usuário** | [02-smoke](./manual/escala/02-smoke-instalador-maquina-limpa.md) · gate **V3.6b** |
| Nesting E.4 vs Aspire | 7/9 peças tamanho OK; posições divergem | ⏸ **V3.2 adiado** — E.4 entregue; teste na máquina depois |
| DXF nesting no menu | Código `ExportCutPlanSheets` + samples | UI **V3.2b** |

---

## Itens antes ➖ — agora no backlog V3 (⬜)

*Decisão 01/07/2026: perseguir paridade completa Promob Plus — ver [ESCOPO-V3](./ESCOPO-V3-PROMOB-COMPLETO.md).*

| ID | Item | Onda V3 |
|----|------|---------|
| **P8** | Diálogo “Deseja fechar a parede…” | V3.1a |
| **F7** | Editar Regiões dentro do editor de faixas | V3.1b |
| **S3** | Abas multi-projeto | V3.1c |
| **L9** | Connect / catálogo online | V3.5b |
| Import `.promob` / SKP | — | V3.4 |
| ERP/fiscal tempo real | Export JSON estático hoje | V3.5d |
| Licenciamento (D5) | Decisão comercial | V3.6a |

---

## Legenda (paridade Promob)

| Símbolo | Significado |
|---------|-------------|
| ✅ | Entregue e documentado |
| 🟡 | Parcial ou validação pendente |
| ⬜ | Backlog V3 |
| ➖ | Diferença aceita permanentemente *(só com decisão explícita — nenhum item ➖ ativo na trilha V3)* |

---

## Validação dos marcos (26/06/2026 – 01/07/2026)

| Check | Status |
|-------|--------|
| `dotnet test` — **398** testes | ✅ |
| `installer/publish.ps1` → `dist/Tracos3DStudio-setup.exe` | ✅ `2026.06.26.2012` |
| Smoke pack | ✅ `dist/Tracos3DStudio-smoke-pack-2026.06.26.2012.zip` |
| Smoke Parte C (Release + fixture + MCP) | ✅ |
| Smoke Parte A (máquina limpa) | 🟡 **usuário** |
| Trilha Promob A–E + V2 | ✅ |
| Documentação V3 consolidada | ✅ [ESCOPO-V3-PROMOB-COMPLETO.md](./ESCOPO-V3-PROMOB-COMPLETO.md) |

---

## Próximo passo (a partir de 01/07/2026)

1. **Seguir [ESCOPO-V3-PROMOB-COMPLETO.md](./ESCOPO-V3-PROMOB-COMPLETO.md)** — ondas V0→V3.6.  
2. **Próximo gate:** **V0.2** ou **V3.3** — **V3.1 ✅** (P8, F7, S3) — **V3.2/CNC na máquina adiado**.  
3. **Agente:** um gate por entrega; loop Promob doc + MCP + manual + screenshots.

*Este documento permanece como registro histórico V1/V2; não adicionar backlog novo aqui — usar ESCOPO-V3.*
