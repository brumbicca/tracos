# Traços 3D — Paredes

**Última revisão:** 21/06/2026  
**Referência Promob:** [Promob - Paredes](https://suporte.promob.com/hc/pt-br/articles/31122539571345-Promob-Paredes)

---

## Neste índice

Documentação de **construção e edição de paredes** no Traços 3D Studio, espelhando o fluxo Promob (construir → medir → editar → encontros).

| # | Artigo | Equivalente Promob (resumo) |
|---|--------|-----------------------------|
| 1 | [Construir paredes](./01-construir-paredes.md) | Construir paredes, com/sem precisão, fechar ambiente |
| 2 | [Orientação e comprimento](./02-orientacao-e-comprimento.md) | Orientação, sentido horário, face interna |
| 3 | [Editor de Paredes](./03-editor-de-paredes.md) | Modo editor 2D / planta dedicada |
| 4 | [Cotas e medidas](./04-cotas-e-medidas.md) | Cotas automáticas, manuais, referência, 30-40-50 |
| 5 | [Encontros e geometria](./05-encontros-geometria.md) | Canto, T, curvas, chanfro, Dry Wall, segmentar, mover |
| 6 | [Camadas, faixas e regiões](./06-camadas-faixas-regioes.md) | Camada, faixas (dois cliques), regiões ret/circ/polígono — screenshots em `docs/screenshots/parede/camadas-faixas/` |
| 7 | [Promob × Traços — lacunas](./07-promob-paridade-camadas-faixas-regioes.md) | Mapeamento doc oficial + backlog Bloco A |

## Status de paridade (resumo)

| Área | Traços 3D |
|------|-----------|
| Comprimento + Orientação (M1–M3) | ✅ |
| Cotas automáticas nos vértices internos (M4) | ✅ |
| Editor de Paredes + cotas manuais (P4, M5) | ✅ |
| Referência na construção (M6) | ✅ |
| 30-40-50 (M7) | ✅ |
| Encontro Canto / T (G1) | ✅ |
| Curvas, segmentar, mover, chanfro, Dry Wall (G2–G6) | ✅ |
| Camadas / faixas / regiões Promob | ✅ A.1–A.5 ([06](./06-camadas-faixas-regioes.md) · [07](./07-promob-paridade-camadas-faixas-regioes.md)); cauda A.6 |

Detalhe técnico e checklist completo: [PLANO-EXECUCAO.md](../../PLANO-EXECUCAO.md) (Fase 1.1 + **Trilha Promob contínua**).

## GIFs desta seção

| GIF | Artigo relacionado |
|-----|-------------------|
| [paredes-construir-horario.gif](../assets/gifs/paredes-construir-horario.gif) | [Construir paredes](./01-construir-paredes.md) |
| [paredes-cota-manual.gif](../assets/gifs/paredes-cota-manual.gif) | [Editor](./03-editor-de-paredes.md), [Cotas](./04-cotas-e-medidas.md) |
| [paredes-mover-particao.gif](../assets/gifs/paredes-mover-particao.gif) | [Encontros — mover](./05-encontros-geometria.md) |
| [paredes-encontro-editor.gif](../assets/gifs/paredes-encontro-editor.gif) | [Encontros](./05-encontros-geometria.md) |

Fixture de teste recomendada: abra `samples/quadrado-5000-horario.tracos` (quadrado 4×5000) ou `samples/quadrado-5000-particao-movel.tracos` (partição móvel).
