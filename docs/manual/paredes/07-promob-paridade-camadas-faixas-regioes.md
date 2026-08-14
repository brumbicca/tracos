# Promob × Traços — Camadas, faixas e regiões

**Última revisão:** 01/07/2026 (V2 + trilha V3)  
**Plano:** [PLANO-EXECUCAO.md](../../PLANO-EXECUCAO.md) · **V3:** [ESCOPO-V3-PROMOB-COMPLETO.md](../../ESCOPO-V3-PROMOB-COMPLETO.md)

Manual Traços de uso: [06-camadas-faixas-regioes.md](./06-camadas-faixas-regioes.md)

---

## Artigos Promob de referência

| Tema | URL |
|------|-----|
| Camadas dos itens | [31122154236177](https://suporte.promob.com/hc/pt-br/articles/31122154236177-Promob-Camadas-dos-itens) |
| Criar faixa na parede | [31121131353745](https://suporte.promob.com/hc/pt-br/articles/31121131353745-Promob-Criar-faixa-na-parede) |
| Criar Regiões | [31121116437649](https://suporte.promob.com/hc/pt-br/articles/31121116437649-KB-Promob-Criar-Regi%C3%B5es) |
| Materiais (parede, faixa, região) | [31121151669009](https://suporte.promob.com/hc/pt-br/articles/31121151669009-Promob-Materiais) |

---

## Camadas

| # | Promob | Traços 3D | Status |
|---|--------|-----------|--------|
| C1 | **Exibir → Janelas → Camadas** + guia lateral Camadas | **Exibir → Camadas...** (`WallLayersWindow`) | ✅ |
| C2 | Ligar/desligar visibilidade por camada | Checkbox por camada de parede | ✅ |
| C3 | Camada em **Propriedades** do item (Parede, Divisória, Referência) | Combo **Camada** em Outras | ✅ |
| C4 | Paredes em camada oculta não renderizam | Implementado | ✅ |
| C5 | **Adicionar camada** nova | **Adicionar camada** na janela Camadas | ✅ A.5 |
| C6 | **Bloquear/desbloquear** camada (módulos não selecionáveis) | Checkbox **Bloqueada** por camada | ✅ A.5 |
| C7 | Modo de preenchimento diferente por camada | Combo **Preenchimento** na janela Camadas (Padrão / Fantasma / Contorno) | ✅ |
| C8 | Camadas para **módulos** e todos os itens 3D | Camada **Módulo** + custom; combo no painel | ✅ A.5 |
| C9 | Remover camadas vazias / sem módulos | **Remover camadas vazias** na janela Camadas | ✅ | A.6a |

---

## Faixas (Editor de Faixas)

| # | Promob | Traços 3D | Status |
|---|--------|-----------|--------|
| F1 | Botão direito na parede → **Editar Faixas** | Menu de contexto no viewport + painel/editor | ✅ |
| F2 | **Editor de Faixas** dedicado (janela/modal) | **Exibir → Editor de Faixas...** + botão no painel (`WallBandsWindow`) | ✅ |
| F3 | Faixa **horizontal** — clique + segundo clique define altura da faixa | **Definir faixa horizontal (dois cliques)** + arraste linhas | ✅ |
| F4 | Faixa **vertical** — clique + segundo clique define largura | **Definir faixa vertical (dois cliques)** + arraste linhas | ✅ |
| F5 | **Múltiplas** faixas H/V arbitrárias na mesma parede | Várias faixas com tamanho definido por dois cliques | ✅ |
| F6 | Arrastar linhas de divisão no editor | Arraste linhas laranja no viewport | ✅ |
| F7 | Botão **Editar Regiões** dentro do editor de faixas | **Editar Regiões...** no `WallBandsWindow` → painel Regiões | ✅ **V3.1b** 01/07/2026 |
| F8 | Material na faixa — arrastar + modo Perfil / Perfil H/V / Face / Tudo | Arraste (Auto ou modo **Faixa**) + combo + preview | ✅ |

---

## Regiões (Editor de Regiões)

| # | Promob | Traços 3D | Status |
|---|--------|-----------|--------|
| R1 | Botão direito → **Editar Regiões** | Menu de contexto → expande **Regiões** no painel | ✅ |
| R2 | Editor em **parede, piso e geometrias** | Regiões no piso: ret/circ/polígono + offset | ✅ A.4 |
| R3 | **Região retangular** — diagonal com mouse | Padrão 1200×1000 ou **dois cliques** nos cantos | ✅ |
| R4 | **Região circular** — clique + raio (Enter) | Clique centro + arraste raio (snap 10 mm) | ✅ |
| R5 | **Região por pontos** — cliques + comprimento Enter + fechar no 1º ponto | **Definir região por pontos** + MeasureBox | ✅ |
| R6 | **Adicionar vértice** na aresta | **Adicionar vértice na aresta** (polígono selecionado) | ✅ | A.6b |
| R7 | **Selecionar/mover** região inteira | Arraste **dentro** da região (bordas = redimensionar) | ✅ | A.6c |
| R8 | **Rotacionar** região (alças pretas) | Alça preta no viewport + **Girar região 90°**; snap 5° | ✅ |
| R9 | **Corte vertical** na região | **Corte vertical** + linha vermelha + **Aplicar** / Enter | ✅ |
| R10 | **Offset** — um lado (setas interna/externa) | **Offset por aresta** (4 campos + setas amarelas no viewport, ±10 mm/clique) | ✅ A.3 |
| R11 | **Offset Forma** — toda a região | Campo **Offset forma (mm)** — uniforme em todas as bordas | ✅ A.3 |
| R12 | Material na região — arrastar + Face / Região / Tudo | Arraste (Auto ou modo **Região**) + combo; preview | ✅ |

---

## Materiais (aplicação)

| # | Promob | Traços 3D | Status |
|---|--------|-----------|--------|
| M1 | Barra/janela de materiais + **arrastar** sobre parede/faixa/região | **Exibir → Materiais...** + drag no viewport | ✅ | [Bloco C](../materiais/07-promob-paridade-materiais.md) C.1–C.2 |
| M2 | Modos: Todo, Face, Perfil, Perfil H/V, Região | Combo **Modo** + auto no drop | ✅ | C.3 |
| M3 | Copiar material entre objetos | Botão **Copiar material** + janela Materiais | ✅ | idem |

---

## Decisão — V1 ✅ (26/06/2026)

**Blocos A, B.2, C, D, E (genérico):** concluídos no escopo V1.

**Backlog V2:** [ESCOPO-V1-VS-PROMOB.md](../../ESCOPO-V1-VS-PROMOB.md) — polish biblioteca (A5, L7), E.4 CNC, etc.

---

## Fixture e regressão

| Artefato | Uso |
|----------|-----|
| `samples/quadrado-5000-camadas-faixas.tracos` | Faixas, regiões ret/circ, materiais |
| `docs/screenshots/parede/camadas-faixas/` | Aceite visual atual |
