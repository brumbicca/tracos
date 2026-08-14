# Traços 3D — Índice rápido do manual

**Última revisão:** 01/07/2026 — **V1+V2 fechados** · **Trilha V3 ativa**  
**Build:** `2026.06.26.2012` · **Testes:** **398**

Mapa de artigos, fixtures e screenshots para aceite visual.

**Marcos:** [ESCOPO-V1-VS-PROMOB.md](../ESCOPO-V1-VS-PROMOB.md) · **Trilha ativa:** [ESCOPO-V3-PROMOB-COMPLETO.md](../ESCOPO-V3-PROMOB-COMPLETO.md)

**Roteiro completo:** [GUIA-INICIO-RAPIDO.md](./GUIA-INICIO-RAPIDO.md)

---

## Por fluxo de trabalho

| Ordem | Fluxo | Índice | Fixture principal |
|-------|--------|--------|-----------------|
| 1 | Construir ambiente | [paredes/README.md](./paredes/README.md) | `samples/quadrado-5000.tracos` |
| 2 | Portas e janelas | [aberturas/README.md](./aberturas/README.md) | `samples/quadrado-5000-porta-janela.tracos` |
| 3 | Módulos | [modulos/README.md](./modulos/README.md) | `fase-2-cozinha-L.tracos` |
| 4 | Camadas, faixas, regiões | [paredes/06-camadas-faixas-regioes.md](./paredes/06-camadas-faixas-regioes.md) | `samples/quadrado-5000-camadas-faixas.tracos` |
| 5 | Orçamento | [orcamento/README.md](./orcamento/README.md) | `fase-2-cozinha-L.tracos` |
| 6 | Detalhamento técnico | [detalhamento/README.md](./detalhamento/README.md) | `fase-2-cozinha-L.tracos` |
| 7 | Produção (corte, E.4 `.tap`) | [producao/README.md](./producao/README.md) | `fase-2-cozinha-L.tracos` |
| 8 | Distribuição / instalador | [escala/README.md](./escala/README.md) | `dist/Tracos3DStudio-setup.exe` |
| 9 | Smoke máquina limpa (opcional) | [escala/02-smoke-instalador-maquina-limpa.md](./escala/02-smoke-instalador-maquina-limpa.md) | gate **V3.6b** |

---

## Screenshots por área

| Pasta | Conteúdo |
|-------|----------|
| `docs/screenshots/parede/` | Paredes, editor, curvas |
| `docs/screenshots/parede/camadas-faixas/` | Camadas, faixas, regiões, materiais |
| `docs/screenshots/aberturas/` | Porta e janela |
| `docs/screenshots/modulos/` | Inserção, colisão, dormitório, A3c |
| `docs/screenshots/orcamento/` | Auditoria, PDF comercial |
| `docs/screenshots/detalhamento/` | Planta cotada, PDF técnico, DXF |
| `docs/screenshots/producao/` | Plano corte, E.4 `.tap`, etiquetas |
| `docs/screenshots/escala/` | Build release |
| `docs/screenshots/aceite-e2e/` | Release E2E + smoke instalador |

---

## Aceite mínimo antes de release (V1/V2)

1. `dotnet test` (`Tracos3DStudio.Tests`) — **398** testes.
2. `installer\publish.ps1` → `dist/last-build.txt` atualizado.
3. Smoke Parte C (Release + fixture) — ✅ 26/06/2026.
4. Smoke Parte A em **máquina limpa** — 🟡 opcional ([02-smoke](./escala/02-smoke-instalador-maquina-limpa.md)) · gate **V3.6b**.

---

## Trilha V3 — próximos gates

Ver [ESCOPO-V3-PROMOB-COMPLETO.md](../ESCOPO-V3-PROMOB-COMPLETO.md).

| Gate | Foco | Nota |
|------|------|------|
| **V3.1c** | S3 — abas multi-projeto | ✅ |
| **V3.1b** | F7 — Editar Regiões no editor faixas | ✅ 01/07/2026 |
| **V0.2** | Auditoria índice Plus | Documentação |
| **V3.2** | CNC / Aspire / chapa | ⏸ adiado — sem teste na máquina agora |
| **V3.3** | Catálogo novos ambientes | Após V3.1 |
| **V3.7** | Engenharia modulação / Construtor | V3.7c ✅ · próximo **V3.7d** |
| **V3.2** | CNC / Aspire / chapa | ⏸ adiado — sem teste na máquina agora |

Ver [COMO-MANTER.md](./COMO-MANTER.md).
