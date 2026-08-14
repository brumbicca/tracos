# Promob × Traços — Materiais e shell (Bloco C)

**Última revisão:** 01/07/2026 (V1 + trilha V3)  
**Plano:** [PLANO-EXECUCAO.md](../../PLANO-EXECUCAO.md) · **V3:** [ESCOPO-V3-PROMOB-COMPLETO.md](../../ESCOPO-V3-PROMOB-COMPLETO.md)

Manual Traços (quando existir): [README.md](./README.md)

---

## Artigos Promob de referência

| Tema | URL |
|------|-----|
| Materiais (parede, faixa, região) | [31121151669009](https://suporte.promob.com/hc/pt-br/articles/31121151669009-Promob-Materiais) |
| Camadas dos itens (contexto) | [31122154236177](https://suporte.promob.com/hc/pt-br/articles/31122154236177-Promob-Camadas-dos-itens) |

---

## Materiais — aplicação

| # | Promob | Traços 3D | Status | Entrega |
|---|--------|-----------|--------|---------|
| M1 | Barra/janela de materiais + **visual** (amostras) | Aba **Materiais** + **Exibir → Materiais...** (`MaterialsPanel`) | ✅ | C.1 + S2 |
| M1b | **Arrastar** material sobre objeto | Arraste da janela Materiais → viewport | ✅ | C.2 |
| M2 | Modos: Todo, Face, Perfil, Perfil H/V, Região | Combo **Modo** na janela Materiais + auto no drop | ✅ | C.2 + C.3 |
| M2b | Seletor explícito de modo na UI | Combo **Modo** (`MaterialsModeCombo`) | ✅ | C.3 |
| M3 | Copiar material entre objetos | Botão **Copiar material** + janela Materiais | ✅ | M3 |
| M4 | Material em **faixa** | Combo + preview + **arrastar** (modo Auto ou Faixa) | ✅ | A.2 + F8 |
| M5 | Material em **região** parede | Combo + preview + **arrastar** (modo Auto ou Região) | ✅ | A.1–A.3 + R12 |
| M6 | Material em **região** piso | Combo + preview | ✅ | A.4 |
| M7 | Material em **módulo** | Combo Materiais no painel | ✅ | Fase 2 |
| M8 | Material no **piso** base | Combo Materiais do piso | ✅ | Fase 1.3 |
| M9 | Material na **face livre** da parede (sem faixa/região) | `InternalFaceMaterialId` / `ExternalFaceMaterialId` + combo no painel | ✅ | C.2.1 |

---

## Shell / Exibir (Bloco C — complementar)

| # | Promob | Traços 3D | Status | Entrega |
|---|--------|-----------|--------|---------|
| S1 | **Exibir → Janelas → Materiais** | **Exibir → Materiais...** (foca guia lateral) | ✅ | C.1 |
| S2 | Guia lateral Materiais (dock) | Aba **Materiais** na coluna Bibliotecas (`MaterialsPanel`) | ✅ | S2 |
| S3 | Abas de projeto na barra superior | Uma janela por projeto | ✅ **V3.1c** |
| S4 | Barra inferior de status rica | Segmentos: projeto · vista/seleção · material/modo · dica · status | ✅ | S4 |

---

## Decisão — V1 ✅ (26/06/2026)

**C.1–C.3 + M3 + R12 + F8 ✅ entregues.** Bloco C fechado no V1.

**Backlog V2:** [ESCOPO-V1-VS-PROMOB.md](../../ESCOPO-V1-VS-PROMOB.md).

---

## Fixture e regressão

| Artefato | Uso |
|----------|-----|
| `samples/quadrado-5000-camadas-faixas.tracos` | Faixas/regiões com materiais distintos |
| `fase-2-cozinha-L.tracos` | Módulos com acabamento |
| `docs/screenshots/materiais/janela-materiais.png` | Aceite C.1 — janela com amostras |
| `docs/screenshots/materiais/fase-C.3-modo-material.png` | Aceite C.3 — combo Modo (Face da parede) |
| `docs/screenshots/materiais/fase-C.2.1-face-livre-frontal.png` | Aceite C.2.1 — face livre pintada (vista Frontal) |
| `docs/screenshots/materiais/fase-C.2.1-face-livre-perspectiva.png` | Aceite C.2.1 — face livre pintada (Perspectiva) |
| `docs/screenshots/materiais/fase-M.3-copiar-material.png` | Aceite M3 — ferramenta Copiar material |
| `docs/screenshots/materiais/fase-F8-drag-faixa.png` | Aceite F8 — arrastar material na faixa |
| `docs/screenshots/materiais/fase-R12-drag-regiao.png` | Aceite R12 — arrastar material na região |
| `docs/screenshots/materiais/fase-S2-dock-materiais.png` | Aceite S2 — guia lateral Materiais (aba dockada) |
| `docs/screenshots/shell/fase-S4-status-bar.png` | Aceite S4 — barra de status segmentada |
