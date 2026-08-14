# Plano de Execução — Traços 3D Studio

**Documento operacional para acompanhamento do desenvolvimento**

| Campo | Valor |
|-------|-------|
| **Versão** | 2.0 |
| **Data** | 01/07/2026 |
| **Marco V1** | [ESCOPO-V1-VS-PROMOB.md](./ESCOPO-V1-VS-PROMOB.md) — feature-complete 26/06/2026 |
| **Marco V2** | Encerrado 26/06/2026 (V2.1–V2.8) |
| **Trilha ativa** | [ESCOPO-V3-PROMOB-COMPLETO.md](./ESCOPO-V3-PROMOB-COMPLETO.md) — paridade Promob Plus completa |
| **PRD associado** | [PRD.md](./PRD.md) |
| **Manual de uso** | [manual/README.md](./manual/README.md) — documentação Promob-like (imagens/GIFs) |
| **Referência** | Promob Plus (projeto/venda) → Promob Start/Maker (produção) |

> **Como usar:** marque `[x]` conforme concluir cada item. **Trilha ativa V3:** [ESCOPO-V3-PROMOB-COMPLETO.md](./ESCOPO-V3-PROMOB-COMPLETO.md). Histórico V1/V2: [ESCOPO-V1-VS-PROMOB.md](./ESCOPO-V1-VS-PROMOB.md).

---

## Visão geral das fases

```
Fase 1 ──► Fase 2 ──► Fase 3 ──► Fase 4 ──► Fase 5 ──► Fase 6
Ambiente   Módulos    Orçamento   Técnico     Produção    Escala
~6–8 sem   ~8–10 sem  ~6–8 sem    ~8–10 sem   ~10–12 sem  futuro
```

| Fase | Nome | Objetivo | Entrega principal |
|------|------|----------|-------------------|
| **1** | Núcleo de ambiente | Ambiente completo e persistência | Projeto salvo com paredes, aberturas e vistas |
| **2** | Biblioteca modular | Móveis parametrizáveis no 3D | 4 módulos de cozinha inseríveis e editáveis |
| **3** | Comercial | Vender o projeto | Orçamento + PDF + imagem |
| **4** | Detalhamento | Fabricar com informação | Planta 2D + lista de peças |
| **5** | Produção | Chão de fábrica | Plano de corte básico |
| **6** | Escala | Empresa / rede | Biblioteca própria, nuvem (opcional) |
| **V4** | Visual / Render | Superar Promob visualmente | Texturas, PBR, sombras, render final — ver [Trilha V4](#trilha-v4--evolução-visual-render) |

**Estimativa total até MVP comercial (Fases 1–3):** ~20–26 semanas  
**Estimativa até produção básica (Fases 1–5):** ~38–48 semanas  

*Estimativas para 1 desenvolvedor em tempo parcial; ajustar conforme equipe.*

---

## Status global do projeto

**Progresso geral:** `██████████` **100%** (release beta `2026.06.26.2012`)

| Área | Progresso | Observação |
|------|-----------|------------|
| UI shell | 100% | Menus principais completos |
| Ambiente / paredes | 100% | Paridade Promob paredes (M1–M4 + checklist visual) |
| Aberturas | 100% | Portas/janelas + manual e aceite visual |
| Módulos / biblioteca | 100% | Cozinha hierárquica (Balcões/Gaveteiros/Cantos/Aéreos/Despenseiro) + **Dormitório (3)** + custom |
| Orçamento / comercial | 100% | PDF Promob, logo, auditoria, D.1–D.3 |
| Detalhamento técnico | 100% | PDF (planta + elevações + peças + furos), DXF planta, **DXF peças com furos** |
| Produção / corte | 100% | MaxRects, etiquetas, E.1–E.4 (`.tap` Jaraguá Mach4) |
| Escala / distribuição | 100% | Biblioteca, ERP JSON, backup ZIP, instalador — **build `2026.06.26.2012`** |
| Persistência | 100% | Projeto + biblioteca + perfil construção + cômodos (V2.7) |
| Arquitetura / refatoração | 100% | RenderEngine + ViewportRenderer + renderers de parede/superfície/abertura/draft |
| Testes automatizados | 100% | **417** testes |
| **Manual / documentação** | 100% | [GUIA-INICIO-RAPIDO.md](./manual/GUIA-INICIO-RAPIDO.md) · V3 [ESCOPO-V3](./ESCOPO-V3-PROMOB-COMPLETO.md) |

**Última atualização deste plano:** 01/07/2026 (**consolidação V3 — documentação + trilha Promob Plus completa**)

---

## Marcos V1 + V2 — encerrados (26/06/2026)

**Histórico V1/V2:** [ESCOPO-V1-VS-PROMOB.md](./ESCOPO-V1-VS-PROMOB.md)

| Campo | Valor |
|-------|-------|
| Build | `2026.06.26.2012` |
| Testes | **398** aprovados |
| Trilha Promob V1 | Blocos **A**, **B**, **C**, **D**, **E.1–E.4** ✅ |
| Backlog V2 | **V2.1–V2.8** ✅ (L7, A3–A5, A8, L10, A3c, E.4) |
| Validação opcional | Smoke Parte A (VM limpa) — gate **V3.6b** |

---

## Trilha V3 — Promob Plus completa (ativa desde 01/07/2026)

**Documento canônico:** [ESCOPO-V3-PROMOB-COMPLETO.md](./ESCOPO-V3-PROMOB-COMPLETO.md)

**Decisão de produto:** fechar **toda** lacuna restante em relação ao Promob Plus — incluindo itens antes ➖ (P8, F7, S3, L9), polish CNC (E.4 vs Aspire), expansão de catálogo, **engenharia de modulação (V3.7)**, intercâmbio `.promob`/SKP e Fase 6 (Connect/nuvem/ERP live).

### Ondas V3 (ordem recomendada)

| Onda | Foco | Gates principais | Status |
|------|------|------------------|--------|
| **V0** | Inventário + docs alinhados | V0.1–V0.4 | 🟡 V0.1/V0.3/V0.4 ✅ · V0.2 índice Plus pendente |
| **V3.1** | Paridade UX (➖ → ✅) | P8 ✅ · F7 ✅ · S3 ✅ | ✅ V3.1 fechada 01/07/2026 |
| **V3.2** | CNC polish E.4 | Aspire, chapa, DXF menu | ⏸ adiado (sem teste máquina) |
| **V3.3** | Catálogo + ambientes | banheiro, lavanderia, cômodo auto, decoração | ⬜ |
| **V3.7** | Engenharia modulação | Construtor, regras `.tracos-lib`, motor paramétrico, **Configurador de Dimensões** | 🟡 V3.7a–c ✅ · V3.7f Fase 1–3c ✅ · V3.7d ✅ · V3.7e ⬜ · **restante Cozinhas/Dormitórios** ⬜ |
| **V3.4** | Intercâmbio Promob | spike `.promob`, import/export, SKP | ⬜ |
| **V3.5** | Plataforma / escala | L9 Connect-like, nuvem, API ERP | ⬜ |
| **V3.6** | Comercial / release | D5 licença, smoke A, release V3 | ⬜ |

**Próximo gate:** **V0.2** (auditoria índice Plus) ou **V3.3** (catálogo/ambientes) — **V3.1 ✅** (P8 · F7 · S3) 01/07/2026.

### Como operar (cada gate V3)

1. Artigo **Promob** (doc oficial) + observação **Promob Plus ao vivo** (WinApp MCP).
2. **Uma lacuna** por entrega — gate nomeado (`V3.2b`, não “produção inteira”).
3. `dotnet test` + ambiente fechado + MCP + screenshots `docs/screenshots/`.
4. Atualizar manual, tabela paridade, **ESCOPO-V3**, **PLANO** e fixture `.tracos`.
5. Marcar gate ✅ neste plano e no ESCOPO-V3.

**Legenda:** ✅ entregue · 🟡 parcial · ⬜ backlog V3 · ➖ só com decisão explícita de não fazer

### Trilha Promob V1/V2 — blocos concluídos (referência)

| Bloco | Foco Promob | Manual / spec | Status |
|-------|-------------|---------------|--------|
| **A** | Paredes — faixas, regiões, camadas | [07 camadas](./manual/paredes/07-promob-paridade-camadas-faixas-regioes.md) | ✅ |
| **B** | Biblioteca / módulos | [07 biblioteca](./manual/modulos/07-promob-paridade-biblioteca.md) | ✅ + V2 |
| **C** | Materiais + shell | [07 materiais](./manual/materiais/07-promob-paridade-materiais.md) | ✅ |
| **D** | Comercial polish | [orcamento/](./manual/orcamento/README.md) | ✅ |
| **E** | Produção E.1–E.4 | [producao/](./manual/producao/README.md) | ✅ E.4 Jaraguá |
| **F (local)** | Distribuição offline | [escala/](./manual/escala/README.md) | ✅ |
| **F (nuvem)** | Connect, ERP live | ESCOPO-V3 §V3.5 | ⬜ |

**Especificação histórica B/C:** [TRILHA-PROMOB-BC-ESPECIFICACAO.md](./TRILHA-PROMOB-BC-ESPECIFICACAO.md)

### Bloco A (paredes) — concluído MVP

| # | Lacuna | Referência | Status |
|---|--------|------------|--------|
| A.1 | Região **poligonal por pontos** | [Promob — Criar Regiões](https://suporte.promob.com/hc/pt-br/articles/31121116437649) | ✅ 21/06/2026 |
| A.2 | Editor de Faixas — fluxo **dois cliques** + múltiplas faixas arbitrárias | [Promob — Criar faixa](https://suporte.promob.com/hc/pt-br/articles/31121131353745) | ✅ 21/06/2026 |
| A.3 | Offset por lado vs Offset Forma (setas na aresta) | Criar Regiões | ✅ 21/06/2026 |
| A.4 | Regiões no **piso** | Criar Regiões | ✅ 21/06/2026 |
| A.5 | Camadas — adicionar/bloquear camada (módulos) | [Camadas dos itens](https://suporte.promob.com/hc/pt-br/articles/31122154236177) | ✅ 21/06/2026 |
| A.6 | Cauda: C9 **ou** R6 **ou** R7 (1 por entrega) | [07-promob-paridade](./manual/paredes/07-promob-paridade-camadas-faixas-regioes.md) | ✅ **A.6a–c** |
| R8 | **Rotacionar** região (alça preta + Girar 90°) | idem | ✅ 23/06/2026 |
| R9 | **Corte vertical** na região | idem | ✅ 23/06/2026 |

### Entrega — Bloco C (materiais) ✅ V1

| # | Lacuna | Referência | Status |
|---|--------|------------|--------|
| C.1 | **Janela de materiais** + preview + material ativo | [Materiais Promob](https://suporte.promob.com/hc/pt-br/articles/31121151669009) | ✅ 19/06/2026 |
| C.2 | **Arrastar** material → faixa/região/piso/módulo | idem | ✅ 19/06/2026 |
| C.2.1 | Material na face livre da parede (opcional) | idem | ✅ |
| C.3 | Seletor explícito de modo de material | idem | ✅ |
| M3 | Copiar material entre objetos | idem | ✅ |
| R12 | Arrastar material na **região** (modo Região + Auto) | idem | ✅ 26/06/2026 |

### Bloco B (biblioteca) — após C.2

| # | Lacuna | Referência | Status |
|---|--------|------------|--------|
| B.1 | Mapeamento Promob × Traços (tabela + escopo B.2) | [07-promob-paridade-biblioteca](./manual/modulos/07-promob-paridade-biblioteca.md) | ✅ 19/06/2026 |
| B.1b | Hierarquia Cozinhas (Inferiores → Balcões/Gaveteiros/Cantos…) | `ModuleLibraryHierarchy` + `CozinhasLibraryHost` | ✅ 14/07/2026 |
| B.1c | Inferiores completo (10 menus + SKUs + ShapeKind 3D) | `ModuleCatalogInferiores` — ordem Promob | ✅ 14/07/2026 |
| B.2 | Abas **Inserir** / **Ambiente** + lista de módulos | idem | ✅ 19/06/2026 |

---

## Trilha pós-MVP (lacunas comerciais e produção)

**Status:** ✅ Concluída (17/06/2026)

| # | Item | Status | Artefato de aceite |
|---|------|--------|-------------------|
| 1 | PNG comercial 2× (só 3D, sem chrome) | ✅ | Menu **Exibir → Exportar PNG apresentação** |
| 2 | PDF orçamento estilo Promob + logo | ✅ | `docs/screenshots/fase-3/fase-3-orcamento.png`, `fase-3-orcamento.pdf` |
| 3 | Etiquetas por peça (PDF) | ✅ | `docs/screenshots/fase-5/fase-5-etiquetas.png`, `fase-5-etiquetas.pdf` |
| 4 | Auditoria pré-orçamento | ✅ | `docs/screenshots/fase-3/fase-3-auditoria.png` |
| 5 | Furos básicos dobradiça em portas | ✅ | `docs/screenshots/fase-5/fase-5-furos-dobradica.png` |
| 6 | Nesting MaxRects + instalador Windows | ✅ | `docs/screenshots/fase-6/fase-6-plano-corte-maxrects.png`, `dist\Tracos3DStudio-setup.exe` |

**Fixture de teste:** `fase-2-cozinha-L.tracos` (cozinha em L, 4 módulos)

**Regenerar instalador:** `powershell -ExecutionPolicy Bypass -File installer\publish.ps1`

---

## Próximos passos sugeridos (trilha Promob B/C)

| Prioridade | Item | Motivo |
|------------|------|--------|
| **1** | ~~**C.1** — Janela de materiais + preview~~ | ✅ **Exibir → Materiais...** |
| **2** | ~~**C.2** — Drag material no viewport~~ | ✅ Arraste da janela Materiais |
| **3** | ~~**B.1** — Mapeamento biblioteca (doc)~~ | ✅ Tabela + escopo B.2 congelado |
| **4** | ~~**B.2** — Abas Inserir / Ambiente + caudas~~ | ✅ L3/L4/L6, A7/A9, S2/S4 |
| **5** | ~~**A.6a** — Remover camada vazia (C9)~~ | ✅ Botão na janela Camadas |
| **6** | ~~**A.6b** — Vértice na aresta (R6)~~ | ✅ Polígono selecionado + clique na aresta |
| **7** | ~~**A.6c** — Mover região inteira (R7)~~ | ✅ Arraste dentro da região |
| — | Smoke instalador | 🟡 **Build `2026.06.26.2012`** — Parte C dev ✅ · Parte A VM limpa pendente |
| **8** | ~~**E.1** — JSON plano de corte (máquina)~~ | ✅ `tracos-cut-plan` + furos |
| **9** | ~~**D.1** — Validade do orçamento no PDF~~ | ✅ Campo dias + cabeçalho PDF |
| **10** | ~~**E.2** — CSV furos CNC (coords na chapa)~~ | ✅ Conversor + menu/janela plano |
| **11** | ~~**D.2** — Desconto + condições de pagamento~~ | ✅ PDF + metadata |
| **12** | ~~**D.3** — Vendedor + observações comerciais~~ | ✅ Cabeçalho + caixa PDF |
| **13** | ~~**E.3** — JSON CNC tracos-cnc-job~~ | ✅ Cortes + furos na chapa |
| — | ~~**Backlog V2**~~ | ✅ **V2.1–V2.8** 26/06/2026 |
| **Ativo** | **Trilha V3** — [ESCOPO-V3-PROMOB-COMPLETO.md](./ESCOPO-V3-PROMOB-COMPLETO.md) | **V3.1** ou V0.2 |

### Gate E.4 — export máquina específica ✅ (26/06/2026)

Gate fechado **5/5** — router **Jaraguá Solid TAF** + Mach4, referência `teste corte.tap` / post `JRGCNC - TAF.pp`.

| # | Pergunta | Go |
|---|----------|-----|
| 1 | Máquina/software alvo definido (marca + modelo)? | ✅ Solid TAF + Mach4 |
| 2 | Amostra de arquivo que a máquina já executou? | ✅ `teste corte.tap` |
| 3 | Quem valida na chapa (operador/integrador)? | ✅ Operador Solid TAF |
| 4 | Formato fechado (TCN/BPP/MPR/G-code **com regras**)? | ✅ `.tap` post JRG |
| 5 | Escopo assinável em uma frase (job mínimo + fixture)? | ✅ cozinha L → contorno + furos T3 |

**Entrega:** [07-export-tap-jaragua-mach4.md](./manual/producao/07-export-tap-jaragua-mach4.md) — menu **Produção → Exportar .tap Jaraguá (Mach4)...**

---

## Fase 1 — Núcleo de ambiente

**Meta:** Usuário cria ambiente fechado, insere portas/janelas, navega nas vistas e salva o projeto.

**Duração estimada:** 6–8 semanas  
**Dependências:** nenhuma  
**Status da fase:** ✅ Concluída (17/06/2026)

### 1.1 Fundação (já entregue)

- [x] Estrutura WPF + OpenGL (`GLWpfControl`)
- [x] Layout Promob-like (menu, toolbar, bibliotecas, propriedades, status)
- [x] Modelo `WallDraft`, `WallSegment`, `Room`, `Geometry2D`
- [x] Desenho de paredes com snap 100 mm e ângulo 45°
- [x] Orientação de parede (**R** = Interna/Externa) e preview fantasma
- [x] Fechamento de ambiente
- [x] Seleção e edição de comprimento de parede
- [x] Delete e Ctrl+Z em paredes
- [x] Câmera perspectiva e ortográfica no modo parede

### 1.2 Aberturas

- [x] UI: botão **Porta** ativa modo inserção em parede
- [x] UI: botão **Janela** ativa modo inserção em parede
- [x] Posicionar abertura por clique ao longo da parede (distância da origem)
- [x] Propriedades: largura, altura, peitoril (janela)
- [x] Renderizar recorte na geometria da parede (buraco visual)
- [x] Recorte em **parede curva** (tesselação do arco + contorno) — fixture `samples/curva-porta.tracos` — *20/06/2026*
- [x] Selecionar e excluir abertura
- [x] Persistir aberturas no modelo `Room` (em memória; salvar arquivo na 1.5)
- [x] **Aceite visual + manual** — fixture `samples/quadrado-5000-porta-janela.tracos`, screenshots `docs/screenshots/aberturas/` — *20/06/2026*

### 1.3 Ambiente completo

- [x] Renderizar `FloorSurface` / malha de piso ao fechar ambiente
- [x] Teto automático ao fechar ambiente (toggle em **Exibir → Alternar teto automático**)
- [x] Barra de status: face/seleção dinâmica (ambiente fechado + seleção parede/abertura)

### 1.4 Navegação e vistas

- [x] Botão **Perspectiva** — restaura câmera 3D livre
- [x] Botão **Planta** — vista superior ortográfica
- [x] Botão **Frontal** — vista frontal ortográfica
- [x] Botão **Esquerda** / **Direita** — vistas laterais
- [x] (P2) Botão **Raio X** — transparência de paredes (básico em perspectiva)

### 1.5 Persistência

- [x] Definir schema `Project` + versão
- [x] Serializar/deserializar JSON (`.tracos`)
- [x] Menu **Novo** — projeto em branco com confirmação se alterado
- [x] Menu **Abrir** — diálogo de arquivo
- [x] Menu **Salvar** / **Salvar como**
- [x] Indicador de projeto alterado (título ou asterisco)

### Promob vs Traços — persistência

Referência estratégica para a Fase 1.5 e decisões futuras de dados.

| Camada | Promob (mercado) | Traços 3D (nosso plano) |
|--------|------------------|-------------------------|
| **Projeto do cliente** | Nuvem **Connect** (versões recentes); exportação **`.promob`** (arquivo proprietário); autosave local temporário | Arquivo **`.tracos`** (JSON + `schemaVersion`) — um documento por projeto |
| **Biblioteca / catálogo** | Bibliotecas de fabricante, itens online (Catalog3D/Decore), **regras de construção** | ✅ hierarquia Cozinhas (L2b 14/07/2026) + `.tracos-lib` · ⬜ **V3.7** engenharia modulação · ⬜ V3.3 outros ambientes |
| **Produção** | Export **`.planner`** → Promob Cut Pro | **Fase 5** — plano de corte / CSV |
| **Comercial (cliente, orçamento)** | Dados no **Connect** (conta, cliente, status) | **Fase 3** — campos no projeto; sem ERP na V1 |
| **Banco SQL** | Não exposto ao projetista; backend da plataforma Promob | **Não na V1** — desktop offline com arquivo |
| **Escala / rede** | Multi-usuário, sincronização, ERP | **Fase 6** (opcional) — nuvem + API + catálogo corporativo |

**Conclusões para o Traços:**

- A 1.5 implementa **documento em arquivo**, não banco de dados — alinhado ao MVP desktop e à decisão **D1** (JSON).
- **Não** ler `.promob` na V1; compatibilidade com Promob seria export/import futuro, se fizer sentido comercial.
- Quando o produto crescer, o `.tracos` permanece como **formato de intercâmbio** (como o `.promob` exportado), e um servidor pode indexar projetos em banco na Fase 6.

**Links Promob:** [Salvar projetos](https://suporte.promob.com/hc/pt-br/articles/31121685244049-Promob-Salvar-Projetos) · [Importar/Exportar](https://suporte.promob.com/hc/pt-br/articles/31121702310417-Promob-Importar-e-Exportar-Projetos)

### Promob vs Traços — paredes

Referência de comportamento a partir do artigo oficial [Promob — Paredes](https://suporte.promob.com/hc/pt-br/articles/31122539571345-Promob-Paredes) e validação no Promob Plus 5.60 (WinApp MCP, 20/06/2026).

**Status Fase 1.1 (paridade mínima Promob):** ✅ **Fechada — 20/06/2026** (M1–M4 + checklist de aceite + teste visual 4×5000)

**Sample de regressão visual:** `samples/quadrado-5000-horario.tracos` — abrir com `dotnet run -- samples/quadrado-5000-horario.tracos`

**Legenda:** ✅ entregue · 🟡 parcial ou em validação · ⬜ não previsto na V1 · ➖ diferença intencional

#### Desenho e fluxo

| # | Comportamento Promob | Traços 3D | Status |
|---|----------------------|-----------|--------|
| P1 | Construir paredes no **ambiente 3D** (clique no piso) | Botão **Parede** + cliques no viewport | ✅ |
| P2 | Construção **com precisão** (digitar comprimento + Enter) | **MeasureBox** + Enter | ✅ |
| P3 | Construção **sem precisão** (arrastar mouse) | Clique no viewport define o segmento | ✅ |
| P4 | **Editor de Paredes** (vista 2D dedicada) | Botão **Editor Paredes** — Planta fixa, módulos/teto ocultos, construção automática | ✅ **20/06/2026** |
| P5 | Snap em grade / incrementos lineares e angulares | Snap **100 mm** + trava **45°** | ✅ |
| P6 | Preview da parede durante desenho | Linha fantasma + tracejado interno | ✅ |
| P7 | Fechar ambiente ao clicar no **ponto inicial** | Fechamento automático (`CloseSmart`) | ✅ |
| P8 | Mensagem *“Deseja fechar a parede…”* ao fechar | Diálogo Sim/Não antes de fechar contorno | ✅ **V3.1a** 01/07/2026 |
| P9 | **Undo** / excluir segmento ou grupo | Ctrl+Z + Delete (face ou grupo) | ✅ |

#### Medida, orientação e cotas

| # | Comportamento Promob | Traços 3D | Status |
|---|----------------------|-----------|--------|
| M1 | Campo **Comprimento** aplica ao lado definido por **Orientação** | `WallMeasureSide` + `BuildWallsFromReferenceCorners`; medida na face de referência | ✅ **20/06/2026** |
| M2 | **Orientação**: lado que recebe o comprimento | Combo **Interna/Externa** no painel Construção + parede selecionada; tecla **R**; `MeasureSide` em `.tracos` | ✅ **20/06/2026** |
| M3 | Sentido **horário** na construção para alinhamentos e cotas internas corretas | Horário + Interna → face interna 5000; anti-horário segue Orientação (158 testes) | ✅ **20/06/2026** |
| M4 | Cotas automáticas nos **vértices internos** (v5.60.16+) | `WallAutomaticDimensionService` + desenho viewport + labels WPF | ✅ **20/06/2026** |
| M5 | Cotas manuais retas/angulares no Editor de Paredes | **Cota Reta** / **Cota Angular** / **Remover Cota** no editor | ✅ **20/06/2026** |
| M6 | Construção com **referência** (parede interna a X mm de outra) | `WallReferenceService` + modo append no desenho | ✅ **20/06/2026** |
| M7 | Ferramenta **30-40-50** (ângulo por três medidas) | Painel **30-40-50** + parede deslocada vermelha | ✅ **20/06/2026** |

#### Geometria e encontros

| # | Comportamento Promob | Traços 3D | Status |
|---|----------------------|-----------|--------|
| G1 | **Encontro de paredes** (canto / T — ferramenta manual) | Editor: **Encontro Canto** / **Encontro T** + chanfro automático em `WallVisualBuilder` | ✅ **19/06/2026** |
| G2 | Paredes **curvas** (flecha, hotpoints, arco) | Flecha + ângulo arco + Mover HotPoint + aberturas no arco | ✅ **20/06/2026** |
| G3 | **Segmentar** parede em múltiplos trechos | Botão **Segmentar parede** + clique no ponto de divisão | ✅ **20/06/2026** |
| G4 | **Movimentar** parede (propriedade Movível + cotas) | `WallMoveService` + arraste na Planta + linha azul | ✅ **20/06/2026** |
| G5 | Paredes **chanfradas** (Aparar Parede) | Painel **Aparar Cantos** + hotpoint laranja | ✅ **20/06/2026** |
| G6 | Tipos: Normal / **Dry Wall** | Combo **Tipo** em Outras + espessura 70 mm + visual claro | ✅ **19/06/2026** |

#### Painel Propriedades (parede selecionada)

| Campo Promob | Traços 3D | Status |
|--------------|-----------|--------|
| Comprimento | `WallLengthBox` (face de **referência** / Orientação) | ✅ |
| Espessura | `WallThicknessBox` | ✅ |
| Pé-direito Inicial / Final | `WallHeightStartBox` / `WallHeightEndBox` | ✅ |
| Ângulo Absoluto / Relativo | `WallAngleAbsoluteBox` / `WallAngleRelativeBox` (somente leitura) | ✅ |
| Afastamento Piso | `WallFloorOffsetBox` | ✅ |
| Cotas Anterior / Posterior / Inferior / Superior | Campos homônimos | ✅ |
| Desenhar Face Inferior | `WallDrawBottomFaceCheck` | ✅ |
| Movível / Visível | `WallIsMovableCheck` / `WallIsVisibleCheck` | ✅ |
| Flecha (curva) | `WallFlechaBox` + hotpoint verde | ✅ |
| Camadas / Faixas / Regiões | Combo Camada + faixa/região + materiais + arraste no viewport + **Exibir → Camadas** | ✅ 20/06/2026 |
| Edição **grupo** (todas as paredes do ambiente) | Clique no topo → pé-direito, espessura e afastamento em grupo | ✅ |

#### Critérios de aceite — paridade mínima Promob (paredes)

Use este checklist antes de considerar a Fase 1.1 “paredes” fechada em relação ao Promob:

- [x] Quadrado **4×5000 mm** desenhado no sentido **horário** + Orientação **Interna** → comprimento/cotas na **face interna** (tracejado por dentro) — *testes unitários + `WallDrawingFlowSimulatorTests`*
- [x] Mesmo quadrado no sentido **anti-horário** → comportamento coerente com **Orientação** (Interna: ref 5000 / interno ~4700; Externa: ambos 5000) — *testes unitários*
- [x] Painel **Comprimento** e título refletem a face onde a medida foi aplicada (geometria real via `GetDisplayReferenceLength`)
- [x] Piso encosta na face interna do ambiente quando desenhado no fluxo horário padrão Promob — *`BuildAutomaticFloorPoints` via `UsesInnerFaceA` + testes `RoomTests`*
- [x] Chanfros nos cantos não distorcem segmentos além da tolerância (**±2 mm**)
- [x] Edição de comprimento no painel respeita **Orientação** da parede (`ApplyReferenceLengthToWall`)
- [x] Seleção por **face** vs **grupo** (topo) comporta-se como no fluxo Promob — *topo horizontal = grupo; face lateral = segmento; painel/título/visual alinhados*
- [x] Teste visual WinApp MCP + screenshots em `docs/screenshots/parede/quadrado-5000/` — *quadrado 4×5000 horário via `samples/quadrado-5000-horario.tracos`; cotas 5000, piso, ambiente fechado*
- [x] **M6** construção com referência — *`samples/quadrado-5000-horario.tracos` + screenshots em `docs/screenshots/parede/referencia-m6/`*
- [x] **G4** movimentar parede móvel — *167 testes; fixture `samples/quadrado-5000-particao-movel.tracos`; screenshots em `docs/screenshots/parede/movimentar-g4/`*

**Decisão Traços (M1–M3) — alinhamento Promob (20/06/2026):**

No Promob, **não** é “sempre interno nos dois sentidos”. Funciona assim:

1. **Orientação** — define **qual lado** da parede recebe o valor digitado em **Comprimento** (interno ou externo em relação ao ambiente).
2. **Sentido horário** — ao construir o contorno, o Promob espera giro **horário** para que alinhamentos, vértices e cotas automáticas batam (tracejado/cotas nos vértices internos *conforme a orientação de criação*).
3. Desenho **anti-horário** ou orientação errada → a mesma cota numérica pode cair na face **externa** (comportamento que você viu nos testes).

**Meta Traços:** replicar esse modelo (Orientação + sentido horário como fluxo principal), não forçar medida interna em ambos os sentidos. Campo **Orientação** no painel **Construção de parede** e na parede selecionada; tecla **R** alterna (implementado 20/06/2026).

**Implementação (20/06/2026):** `WallMeasureSide`, `WallInnerFaceService.BuildWallsFromReferenceCorners`, preview/tracejado por winding + Orientação, **158 testes** passando.

### Próxima trilha Promob — paredes avançadas (prioridade sugerida)

| Prioridade | Item | Motivo |
|------------|------|--------|
| **1** | ~~**M6** — construção com referência~~ ✅ | Concluído 20/06/2026 — `WallReferenceService`, modo append, linha azul |
| **2** | ~~**G4** — movimentar parede~~ ✅ | Concluído 20/06/2026 — arraste na Planta + cota azul |
| **3** | ~~**P4** — Editor de Paredes~~ ✅ + ~~**M5** cotas manuais~~ ✅ | Concluídos 20/06/2026 |
| **4** | ~~**G3** — segmentar parede~~ ✅ | Concluído 20/06/2026 — pé-direito variável por trecho |
| **5** | ~~**M7** — 30-40-50~~ ✅ | Concluído 20/06/2026; G2 pendente |
| **6** | ~~**G5** — Aparar Parede~~ ✅ | Concluído 20/06/2026 — chanfro manual nos cantos |
| **7** | ~~**G2** — paredes curvas~~ ✅ | Concluído 20/06/2026 — flecha, ângulo arco, hotpoint |

**Links Promob:** [Índice Plus](https://suporte.promob.com/hc/pt-br/articles/31123224474257-Plus) · [Paredes](https://suporte.promob.com/hc/pt-br/articles/31122539571345-Promob-Paredes) · [Parede interna (KB)](https://suporte.promob.com/hc/pt-br/articles/31119004479249-KB-Dica-Promob-Como-construir-parede-interna) · [Parede chanfrada (KB)](https://suporte.promob.com/hc/pt-br/articles/31119056593169-KB-Dica-Promob-Como-construir-parede-chanfrada)

### 1.6 Refatoração técnica (paralelo)

- [x] Extrair `CameraController` de `MainWindow.xaml.cs`
- [x] Extrair `ViewportRenderer` (draw calls OpenGL — piso, grade, frame)
- [x] Remover lista legada `Wall`; usar só `Room` / `WallSegment`
- [x] Remover `Class1.cs` do repositório
- [x] **OpenGL 3.3 Core** — `RenderEngine` + `GlShaderProgram` (shaders GLSL 330, VAO/VBO, uniform MVP)
- [x] Migrar desenho de viewport: sem `GL.Begin` / `GL.LoadMatrix` (paredes, módulos, grid, preview)

### Critérios de conclusão da Fase 1

- [x] Usuário desenha cozinha retangular fechada em &lt; 5 min
- [x] Insere pelo menos 1 porta e 1 janela
- [x] Salva, fecha app, reabre e geometria idêntica
- [x] Alterna entre planta e perspectiva sem perda de dados

---

## Fase 2 — Biblioteca e módulos

**Meta:** Inserir e editar módulos de cozinha paramétricos no ambiente 3D.

**Duração estimada:** 8–10 semanas  
**Dependências:** Fase 1 concluída (persistência + ambiente)  
**Status da fase:** ✅ Concluída (17/06/2026)

### 2.1 Modelo de módulos

- [x] Classe `ModuleDefinition` (template da biblioteca)
- [x] Classe `ModuleInstance` (instância no projeto)
- [x] Parâmetros: largura, altura, profundidade
- [x] Regras min/max por tipo de módulo
- [x] `MeshData` gerado a partir dos parâmetros (caixas + frentes)
- [x] Integrar instâncias em `Project`

### 2.2 Renderização de módulos

- [x] Desenhar `ModuleInstance` no viewport
- [x] Picking 3D ou projeção para seleção
- [x] Destaque visual do módulo selecionado
- [x] Atualizar malha ao alterar dimensões

### 2.3 Biblioteca — Cozinhas

- [x] **Balcão 2 portas** — inserção e parametria
- [x] **Balcão 3 portas**
- [x] **Gaveteiro**
- [x] **Aéreo**
- [x] Drag ou clique-para-posicionar no ambiente
- [x] Snap em parede (encostar no fundo)

### 2.4 Painel Propriedades (ligação real)

- [x] Dimensões → edita módulo selecionado
- [x] Posicionamento → X, Y, Z ou distância da parede
- [x] Excluir módulo (Delete)
- [x] Rotacionar 90° (atalho ou painel Movimentação)

### 2.5 Colisão e validação

- [x] Detecção básica de sobreposição entre módulos
- [x] Toggle Colisão ON/OFF na status bar
- [x] Aviso visual em colisão (contorno e malha vermelhos)

### 2.6 Persistência

- [x] Salvar/carregar módulos no `.tracos`

### Critérios de conclusão da Fase 2

- [x] Cozinha em L com 4 tipos de módulo posicionados
- [x] Alterar largura do balcão atualiza 3D e propriedades
- [x] Projeto com módulos salvo e reaberto corretamente
- [x] **Face interna + cotas** — preview azul encosta no fundo na face interna; painel Cotas fecha com comprimento interno (`ModuleInnerFaceAcceptanceTests`, fixture `fase-2-cozinha-L.tracos`) — *20/06/2026*
- [x] **Manual colisão + dormitório** — `02-colisao-e-exclusao.md`, `03-modulos-dormitorio.md`, fixtures `samples/dormitorio-quadrado.tracos`, `samples/colisao-modulos.tracos` — *20/06/2026*
- [x] **Manual orçamento + exportação** — `docs/manual/orcamento/`, screenshots `docs/screenshots/orcamento/` — *20/06/2026*
- [x] **Manual produção** — `docs/manual/producao/`, screenshots `docs/screenshots/producao/` — *20/06/2026*
- [x] **Aceite E2E automatizado** — `EndToEndAcceptanceTests` (salvar/reabrir + orçamento + corte + etiquetas PDF/CSV) — *20/06/2026*
- [x] **Correção** `Project.ImportFrom` preserva `AttachedWallId` e `DistanceAlongWall` ao reabrir `.tracos` — *20/06/2026*

---

## Fase 3 — Apresentação e orçamento

**Meta:** Gerar proposta comercial a partir do projeto 3D.

**Duração estimada:** 6–8 semanas  
**Dependências:** Fase 2  
**Status da fase:** ✅ Concluída (17/06/2026)

### 3.1 Materiais

- [x] Cadastro básico de materiais (nome, cor, preço/m² ou preço fixo)
- [x] Aplicar material em módulo (painel Materiais)
- [x] Materiais padrão: MDF branco, MDF madeirado, etc.

### 3.2 Exportação visual

- [x] Exportar PNG do viewport (resolução atual do controle)
- [x] Exportar PNG apresentação **2×**, só 3D (sem chrome da UI)
- [x] Exportar **PNG planta com cotas** (`TechnicalFloorPlanPngExporter`)

### 3.3 Lista e orçamento

- [x] Menu **Orçamento** — painel ou janela de lista
- [x] Listar módulos + dimensões + material
- [x] Tabela de preços editável (configuração)
- [x] Cálculo: total por item e total geral
- [x] Campos cliente expandidos (nome, telefone, e-mail, endereço, CPF/CNPJ)
- [x] (P2) Auditoria pré-orçamento (`BudgetAuditWindow`)

### 3.4 Relatório

- [x] Gerar PDF: capa, imagem 3D, tabela de itens, total
- [x] Layout estilo Promob (`BudgetPdfExporter`)
- [x] Logo da empresa configurável (biblioteca `.tracos-lib`)

### Critérios de conclusão da Fase 3

- [x] Projeto de cozinha gera orçamento em &lt; 1 min após modelagem
- [x] PDF entregue ao cliente com imagem e valores

---

## Fase 4 — Detalhamento técnico

**Meta:** Documentação para marcenaria executar o projeto.

**Duração estimada:** 8–10 semanas  
**Dependências:** Fase 3  
**Status da fase:** ✅ Concluída (17/06/2026)

### 4.1 Decomposição em peças

- [x] Regras: balcão → laterais, base, tampo, fundo, prateleiras, frentes
- [x] Espessuras configuráveis (15, 18, 25 mm)
- [x] Lista de peças com L × A × espessura × qtd × material × **furos**

### 4.2 Desenho 2D

- [x] Vista planta com cotas do ambiente
- [x] Vista frontal da cozinha (elevação por orientação)
- [x] Export imagem ou PDF técnico

### 4.3 Export CAD

- [x] Export DXF (plantas ou contornos de peças)
- [x] Export DXF peças com furos (camadas `PECAS` + `FUROS`) — *20/06/2026*
- [x] Import DXF planta (`DxfImporter` — entidades LINE)

### Critérios de conclusão da Fase 4

- [x] Lista de peças de cozinha teste bate com módulos 3D
- [x] Planta PDF com cotas utilizável na marcenaria

---

## Fase 5 — Produção

**Meta:** Plano de corte e dados para fabricação.

**Duração estimada:** 10–12 semanas  
**Dependências:** Fase 4  
**Status da fase:** ✅ Concluída (17/06/2026)

### 5.1 Regras produtivas

- [x] Cadastro espessura chapa e fita de borda
- [x] Aplicar fita nas arestas expostas (regra simples)
- [x] Furos padrão dobradiça em portas (`DoorHingeDrillingService`, Ø35 mm)
- [x] Furos minifix básicos (`MinifixDrillingService` — cabo Ø5 + excêntrico Ø15)

### 5.2 Plano de corte

- [x] Chapa padrão 2750 × 1850 mm (configurável)
- [x] Nesting **MaxRects** (`RectangleBinPack.CSharp`) — 4 chapas vs 5 greedy na cozinha teste
- [x] Visualização do plano de corte na tela
- [x] Export CSV para otimizador

### 5.3 Menu Produção

- [x] Relatório de consumo de chapas
- [x] Etiquetas por peça (PDF, 10/página A4)

### Critérios de conclusão da Fase 5

- [x] Projeto teste gera plano de corte executável
- [x] Aproveitamento documentado em caso padrão

---

## Fase 6 — Escala (opcional / longo prazo)

**Meta:** Operação multi-usuário, bibliotecas corporativas e distribuição.

**Duração estimada:** a definir  
**Dependências:** Fases 1–5 estáveis  
**Status da fase:** ✅ Distribuição local concluída (20/06/2026) — nuvem/API em backlog

- [x] Editor de biblioteca própria (módulos custom via `.tracos-lib`)
- [x] Sincronização de catálogo e preços (import/export arquivo)
- [x] Backup local (ZIP projeto + biblioteca)
- [x] Export JSON para ERP externo
- [x] Perfis de construção (Padrão / Reforçado / Econômico)
- [x] Instalador Windows (Inno Setup 6 → `dist\Tracos3DStudio-setup.exe`)
- [x] Módulos de dormitório: **Guarda-roupa 2P**, **Criado-mudo 2G**, **Cômoda 4G**
- [ ] (Futuro) Multi-usuário / projetos em nuvem
- [ ] (Futuro) API REST ERP em tempo real

---

## Cronograma sugerido (referência)

| Período | Foco | Marco |
|---------|------|-------|
| Sem 1–2 | Fase 1.2 Aberturas | Porta/janela funcionando |
| Sem 3–4 | Fase 1.3–1.4 Piso + vistas | Ambiente navegável |
| Sem 5–6 | Fase 1.5 Persistência + refatoração | `.tracos` estável |
| Sem 7–10 | Fase 2.1–2.3 Modelo + 1º módulo | Balcão 2 portas |
| Sem 11–14 | Fase 2.3–2.4 Biblioteca completa | 4 módulos cozinha |
| Sem 15–18 | Fase 3 Orçamento + PDF | **MVP comercial** |
| Sem 19–26 | Fase 4 Detalhamento | Lista de peças + planta |
| Sem 27+ | Fase 5 Produção | Plano de corte |
| Pós-MVP | Lacunas comerciais + MaxRects + instalador | **Trilha 1–6 concluída** |

---

## Checklist de qualidade (cada entrega)

Use em toda feature antes de marcar como concluída:

- [ ] Funciona em projeto novo e projeto reaberto
- [ ] Unidades em mm consistentes
- [ ] Sem regressão no desenho de paredes
- [ ] UI em português (BR)
- [ ] Código sem duplicar lógica já existente em `Geometry2D` / domínio
- [ ] Teste visual completo via WinApp MCP (ambiente fechado + controles da entrega + screenshots)

---

## Trilha V4 — Evolução Visual (Render)

> **Contexto:** O Traços usa OpenTK (OpenGL 3.3 Core) com shaders GLSL. O Promob Plus usa viewport OpenGL básico para projeto e render externo (Artlantis/Cinema 4D) para apresentação. A estratégia abaixo supera o Promob em qualidade visual progressivamente, sem trocar o motor nem reescrever a stack.

### Por que não trocar o motor agora

| Motor alternativo | Benefício | Custo real |
|---|---|---|
| Vulkan (via OpenTK 5) | Performance máxima, controle total | Reescrita completa de shaders + pipeline — meses |
| Filament (Google) | PBR pronto, usado no Android/web | Sem binding .NET maduro — integração complexa |
| Cycles (Blender) | Fotorrealismo de referência | Render offline separado — não serve para viewport |
| Three.js / WebGPU | Futuro SaaS/web | Muda stack inteira para Electron/Blazor |

**Conclusão:** evoluir o OpenGL atual entrega 90% do ganho visual com 10% do esforço. Migração de motor fica para V5+ (produto consolidado).

---

### Fase V4.1 — Materiais e Texturas no Viewport ⬜

> **Impacto:** transformação visual imediata — usuário vê MDF/madeira/vidro nas peças

| Item | Descrição | Prioridade |
|------|-----------|-----------|
| V4.1a | Sistema de materiais: `MaterialLibrary` (nome, cor, textura UV, roughness) | Alta |
| V4.1b | Carregamento de texturas PNG/JPG via OpenGL (`TextureCache`) | Alta |
| V4.1c | Shader de textura UV nas faces das chapas (lateral, base, fundo, prateleira) | Alta |
| V4.1d | Mapeamento UV automático por face (planar, sem distorção) | Alta |
| V4.1e | Biblioteca de materiais padrão: MDF branco, MDF madeira, vidro, inox | Alta |
| V4.1f | Seletor de material por peça no painel de propriedades | Média |
| V4.1g | Persistência do material no `.tracos` | Média |

---

### Fase V4.2 — Iluminação PBR Básica ⬜

> **Impacto:** profundidade e realismo sem render externo — supera visualmente o Promob Plus

| Item | Descrição | Prioridade |
|------|-----------|-----------|
| V4.2a | Shader Blinn-Phong melhorado: luz direcional + ambiente + specular por material | Alta |
| V4.2b | Múltiplas fontes de luz configuráveis (sol/teto/ponto) | Média |
| V4.2c | Shadow map básico (sombras projetadas por módulos e paredes) | Média |
| V4.2d | Ambient Occlusion em tela (SSAO) — escurece frestas entre peças | Média |
| V4.2e | Reflexo básico em superfícies brilhantes (vidro, inox, lacado) | Baixa |

---

### Fase V4.3 — PBR Completo + IBL ⬜

> **Impacto:** qualidade fotorrealística no viewport — diferencial competitivo forte

| Item | Descrição | Prioridade |
|------|-----------|-----------|
| V4.3a | Shader PBR: `metallic`, `roughness`, `albedo`, `normal map` por material | Média |
| V4.3b | Image-Based Lighting (IBL): HDRI de ambiente para reflexos realistas | Média |
| V4.3c | Bloom + tone mapping HDR para brilhos suaves | Baixa |
| V4.3d | Editor de material com preview em tempo real (esfera ou cubo de amostra) | Baixa |

---

### Fase V4.4 — Render Final de Apresentação ⬜

> **Impacto:** imagem de proposta comercial para o cliente — fecha venda

| Item | Descrição | Prioridade |
|------|-----------|-----------|
| V4.4a | Export da cena para **glTF 2.0** (padrão aberto, importável no Blender/web) | Média |
| V4.4b | Integração com render offline: botão "Renderizar" abre Blender via CLI | Baixa |
| V4.4c | Galeria de renders salvos no projeto | Baixa |
| V4.4d | Export de imagem de alta resolução diretamente do viewport (sem render externo) | Alta |

---

### Decisão técnica futura — V5+

| # | Cenário | Gatilho recomendado |
|---|---------|---------------------|
| Migrar para Vulkan | Performance cai com >500 módulos na cena | Medir FPS; migrar só se necessário |
| Migrar para Web/SaaS | Demanda de acesso remoto ou multiusuário | Arquitetura separada (não reescrever o desktop) |
| Motor de render dedicado | Cliente pede imagens fotorrealísticas no plano comercial | Integrar Blender CLI ou LuxCoreRender |

---

## Riscos e decisões em aberto

| # | Decisão | Opções | Recomendação |
|---|---------|--------|--------------|
| D1 | Formato `.tracos` | JSON vs MessagePack | JSON (debug fácil) |
| D2 | Renderização módulos | OpenGL imediato vs VBO | ✅ **OpenGL 3.3 Core** + VBO/VAO (`RenderEngine`, 17/06/2026) |
| D3 | PDF | Biblioteca (QuestPDF, iText) | QuestPDF (licença amigável) |
| D4 | Nesting | Próprio vs integrar | ✅ **RectangleBinPack.CSharp** (MaxRects) |
| D5 | Licenciamento | Perpétuo vs assinatura | **Pendente decisão comercial** (fora do escopo técnico) |
| D6 | Evolução visual (render) | Trocar motor vs evoluir OpenGL | ✅ **Evoluir OpenGL** em 4 fases (V4.1→V4.4) — motor novo apenas em V5+ |

---

## Registro de marcos (preencher ao concluir)

| Marco | Data prevista | Data real | Observações |
|-------|---------------|-----------|-------------|
| Fase 1 concluída | — | 17/06/2026 | Ambiente + persistência |
| Primeiro módulo inserível | — | 17/06/2026 | Balcão 2 portas |
| MVP comercial (Fase 3) | — | 17/06/2026 | Orçamento + PDF |
| Detalhamento (Fase 4) | — | 17/06/2026 | Lista de peças + planta |
| Produção básica (Fase 5) | — | 17/06/2026 | Plano de corte + etiquetas |
| Trilha pós-MVP (6 itens) | — | 17/06/2026 | MaxRects + instalador validado |
| Instalador Windows | — | 17/06/2026 | `dist\Tracos3DStudio-setup.exe` testado |
| P2 fechamento (sem nuvem) | — | 17/06/2026 | Minifix, dormitório, teto, import DXF, PNG cotas |
| OpenGL 3.3 Core (D2) | — | 17/06/2026 | `RenderEngine`, shaders, VAO/VBO; instalador regenerado |
| **Paridade Promob paredes (M1–M4)** | — | 20/06/2026 | Orientação, cotas automáticas, face/grupo, sample 4×5000 |
| **Marco V1 feature-complete** | — | 26/06/2026 | ESCOPO-V1, 353→398 testes |
| **Backlog V2 fechado** | — | 26/06/2026 | V2.1 L7 … V2.8 E.4 `.tap` Jaraguá |
| **Trilha V3 documentada** | — | 01/07/2026 | ESCOPO-V3-PROMOB-COMPLETO.md |
| **V3.7 engenharia modulação** | V3.7b ✅ | 02/07/2026 | `ModulationEditorWindow` · biblioteca · screenshots `V3.7b/` |
| **V3.7f Configurador de Dimensões (Fase 1)** | V3.7f ✅ | 03/07/2026 | `DimensionConfiguratorWindow` · Medidas Máximas · Cozinhas/Dormitórios/Painéis Dimensões Externas · persistência no `.tracos` · aplicar inserção/existentes · paridade [Promob Configurador](https://suporte.promob.com/hc/pt-br/articles/31118437369617) |
| **V3.7f Configurador de Dimensões (Fase 2)** | V3.7f.2 ✅ | 03/07/2026 | Chapas · Montagem caixa (Cozinhas/Dormitórios) · Frentes\|Portas · Gavetas · overlay em `CreateEffectiveRules` · sync `PanelThicknessMm`/`BackThicknessMm` · testes `DimensionConfiguratorServiceTests` |
| **V3.7f Configurador (Fase 3a)** | V3.7f.3a ✅ | 03/07/2026 | Dimensões Externas **completas** Cozinhas (A–O) + Dormitórios (A–J) · slots aéreo baixo/médio/alto · cômoda→Bancadas · manual `09-configurador-dimensoes.md` |
| **V3.7f Configurador (Fase 3b)** | V3.7f.3b ✅ | 03/07/2026 | Chapas árvore por peça · `ChapaConfiguratorService` · B/C/D por tipo · persistência `cozinhaChapas`/`dormitorioChapas` |
| **V3.7f Configurador (Fase 3b.2)** | V3.7f.3b.2 ✅ | 03/07/2026 | Chapas **completo**: subárvores Componentes (13) + Gavetas (5) Cozinhas/Dormitórios · chaves `comp-*`/`gav-*` · validado ao vivo (MCP) + screenshot |
| **V3.7f Configurador (Fase 3c.1)** | V3.7f.3c.1 ✅ | 03/07/2026 | Montagem caixa **fatia inicial** · `BoxAssemblyConfiguratorService` · 5 tipos fundo · nós Fundo/Fixação Lateral-Base/Sarrafo/Prateleira · mesh 3D · **Raio X revela interior dos módulos** |
| **V3.7f Configurador (Fase 3d)** | V3.7f.3d ✅ | 03/07/2026 | Montagem Caixa - **Superior completa** (13 folhas) · `BoxAssemblySuperiorSchema` · persistência `superiorNumeric`/`superiorChoice` · mapa Promob ao vivo · validado MCP |
| **V3.7f Configurador (Fase 3e)** | V3.7f.3e ✅ | 03/07/2026 | Montagem Caixa - **Despenseiros \| Torres completa** (9 folhas) · `BoxAssemblyDespenseirosSchema` · `cozinhaDespenseiroBox` · slot `CozinhaDespenseiro` dedicado · validado MCP |
| **V3.7f Configurador (Fase 3f)** | V3.7f.3f ✅ | 03/07/2026 | **Eletros** — folha única (13 campos A–M) · `CozinhaEletrosSchema` · `cozinhaEletros` · validado MCP |
| **V3.7f Configurador (Fase 3g)** | V3.7f.3g ✅ | 04/07/2026 | **Frentes \| Portas** — 9 folhas (7 + Folgas Painel 2) · `CozinhaFrentesPortasSchema` · `cozinhaFrentesPortas` · sync `cozinhaDoorFrontGapMm` · validado MCP |
| **V3.7f Configurador (Fase 3h)** | V3.7f.3h ✅ | 08/07/2026 | **Gavetas** — 4 folhas / 22 combos · `CozinhaGavetasSchema` · `cozinhaGavetas` · validado MCP |
| **V3.7f Configurador (Fase 3i)** | V3.7f.3i ✅ | 09/07/2026 | **Gavetas Internas \| Auxiliares** — 4 folhas / 24 combos · `CozinhaGavetasInternasSchema` · `cozinhaGavetasInternas` · validado MCP |
| **V3.7f Configurador (Fase 3j)** | V3.7f.3j ✅ | 09/07/2026 | **Cozinhas Cava** — 10 folhas / 51 campos · `CozinhaCavaSchema` · `cozinhaCava` · validado MCP |
| **V3.7f Overlay 3D — Sarrafos** | V3.7f.overlay ✅ | 09/07/2026 | Sarrafo dianteiro/traseiro individuais no 3D · orientação H/V independente por sarrafo (`FrontSarrafoIsVertical`/`BackSarrafoIsVertical`) · profundidade individual (`sar-prof-fro`/`sar-prof-tra`) · recuo opcional dianteiro · alinhamento rente às extremidades da lateral · configurador aplicado desde a inserção · persistência global de padrões (`UserDefaultsService` → `%AppData%\Tracos3DStudio\user_defaults.json`) |
| **V3.7f Configurador (Fase 3k — Dormitórios Montagem Completa)** | V3.7f.3k ✅ | 10/07/2026 | **Dormitórios** completo: Montagem de Caixa - Bancadas \| Criados (InferiorSchema) · Montagem de Caixa - Superior (SuperiorSchema) · Frentes \| Portas · Gavetas · `DormitorioBancadaCriadoBox` / `DormitorioSuperiorBox` / `DormitorioFrentesPortas` / `DormitorioGavetas` · slots separados por tipo de módulo · `EnsureDormitorioInitialized` · validado MCP ao vivo |
| **V3.7f Overlay 3D — Dormitórios** | V3.7f.overlay.dor ⬜ | — | Aplicar configurações no motor 3D para módulos Dormitório (mesmo overlay já feito em Cozinhas) |
| **V3.7d Usinagem/fita por template** | V3.7d ✅ | 03/07/2026 | `ModulationEdgeBanding` + `ModulationDrillingPattern` em `ModulationPieceRule` · fita/furos na decomposição · fallback legado · preset `CreateStandardBox` · fixture `modulacao-balcao-regras.tracos-lib` · `ModulationMachiningTests` |
| **V3.1a P8 — diálogo fechar parede** | — | 01/07/2026 | `WallCloseConfirmation` + Sim/Não no fechamento |
| **V3.1b F7 — Editar Regiões no editor faixas** | — | 01/07/2026 | `WallBandsEditRegionsButton` → painel Regiões |

---

## Links

- [PRD completo](./PRD.md)
- [Escopo V3 — Promob Plus completo](./ESCOPO-V3-PROMOB-COMPLETO.md)
- [Escopo V1/V2 (histórico)](./ESCOPO-V1-VS-PROMOB.md)
- [Promob — plataforma](https://promob.com/promob/)
- [Promob Plus — documentação](https://suporte.promob.com/hc/pt-br/articles/31123224474257-Plus)
- [Promob — Paredes](https://suporte.promob.com/hc/pt-br/articles/31122539571345-Promob-Paredes)

---

*Atualize este documento ao final de cada sprint ou entrega significativa.*