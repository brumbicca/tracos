# Manual Traços 3D Studio

**Ajuda para uso do software** — no mesmo espírito da [Central de Ajuda Promob](https://suporte.promob.com/hc/pt-br), mas descrevendo **o que o Traços 3D já entrega hoje** e atualizado conforme o desenvolvimento avança.

| Campo | Valor |
|-------|-------|
| **Última revisão** | 01/07/2026 — **V1+V2 fechados** · trilha **V3** ativa (`2026.06.26.2012`) |
| **Marco V1/V2** | [ESCOPO-V1-VS-PROMOB.md](../ESCOPO-V1-VS-PROMOB.md) |
| **Trilha V3 (ativa)** | [ESCOPO-V3-PROMOB-COMPLETO.md](../ESCOPO-V3-PROMOB-COMPLETO.md) |
| **Referência Promob** | [Plus — índice](https://suporte.promob.com/hc/pt-br/articles/31123224474257-Plus) · [Paredes](https://suporte.promob.com/hc/pt-br/articles/31122539571345-Promob-Paredes) |
| **Plano interno** | [PLANO-EXECUCAO.md](../PLANO-EXECUCAO.md) |

---

## Como usar este manual

1. Escolha o tópico no índice abaixo.
2. Siga o passo a passo com os **nomes exatos** de botões e painéis (iguais na interface).
3. As imagens vêm de capturas de teste em `docs/screenshots/`; GIFs animados ficam em `docs/manual/assets/gifs/` quando disponíveis.
4. Se algo na interface mudou, confira a data **Última revisão** no topo de cada artigo.

> **IMPORTANTE** — Este manual é **documentação de uso**, não substitui o PRD nem o plano de execução para desenvolvedores.

---

## Comece aqui

- **[Guia de início rápido](./GUIA-INICIO-RAPIDO.md)** — roteiro ponta a ponta (instalação → orçamento → corte)
- **[Escopo V1/V2 vs Promob](../ESCOPO-V1-VS-PROMOB.md)** — marcos encerrados
- **[Escopo V3 — Promob Plus completo](../ESCOPO-V3-PROMOB-COMPLETO.md)** — trilha ativa a partir de 01/07/2026
- **[Índice rápido](./INDICE-RAPIDO.md)** — mapa de artigos, fixtures e checklist de release

---

## Índice

### Projeto e arquivos

| Artigo | Conteúdo | Status Traços |
|--------|----------|----------------|
| [Abas multi-projeto (S3)](./projeto/01-abas-multi-projeto.md) | Vários `.tracos` na mesma janela | ✅ V3.1c |

### Construção do ambiente

| Artigo | Conteúdo | Status Traços |
|--------|----------|----------------|
| [Paredes — visão geral](./paredes/README.md) | Índice da seção | ✅ Paridade Promob (Fase 1.1) |
| [Construir paredes](./paredes/01-construir-paredes.md) | Desenho, fechamento, vistas | ✅ |
| [Orientação e comprimento](./paredes/02-orientacao-e-comprimento.md) | Interna/Externa, sentido horário | ✅ |
| [Editor de Paredes](./paredes/03-editor-de-paredes.md) | Vista 2D dedicada, construção no editor | ✅ |
| [Cotas e medidas](./paredes/04-cotas-e-medidas.md) | Automáticas, manuais, 30-40-50, referência | ✅ |
| [Encontros e geometria](./paredes/05-encontros-geometria.md) | Canto, T, curvas, chanfro, Dry Wall, segmentar, mover | ✅ |
| [Camadas, faixas e regiões](./paredes/06-camadas-faixas-regioes.md) | Camada, faixa, região retangular, materiais | ✅ MVP |
| [Materiais — visão geral](./materiais/README.md) | Janela + arrastar no viewport | ✅ C.1–C.2 |

**GIFs:** `docs/manual/assets/gifs/` (4 animações da seção paredes)

### Ambientação

| Artigo | Conteúdo | Status Traços |
|--------|----------|----------------|
| [Portas e janelas](./aberturas/README.md) | Porta/janela na face, propriedades, persistência | ✅ |
| [Inserir porta e janela](./aberturas/01-inserir-porta-janela.md) | Passo a passo | ✅ |
| [Módulos — visão geral](./modulos/README.md) | Biblioteca Cozinhas + Dormitórios, face interna | ✅ |
| [Inserir na face interna](./modulos/01-inserir-na-face-interna.md) | Preview, cotas no painel | ✅ |
| [Colisão e exclusão](./modulos/02-colisao-e-exclusao.md) | Toggle Colisão, Delete | ✅ |
| [Módulos de dormitório](./modulos/03-modulos-dormitorio.md) | Guarda-roupa, criado-mudo, cômoda | ✅ |
| [Engenharia de modulação (V3.7)](./modulos/08-engenharia-modulacao-construtor.md) | Construtor Promob — regras configuráveis | 🟡 V3.7a–c ✅ · V3.7d ⬜ |

### Comercial

| Artigo | Conteúdo | Status Traços |
|--------|----------|----------------|
| [Orçamento — visão geral](./orcamento/README.md) | Auditoria, PDF comercial, exportação PNG | ✅ |
| [Auditoria e orçamento](./orcamento/01-auditoria-e-orcamento.md) | Passo a passo | ✅ |
| [Exportação visual](./orcamento/02-exportacao-visual.md) | Viewport, apresentação 2×, planta cotas | ✅ |

### Produção

| Artigo | Conteúdo | Status Traços |
|--------|----------|----------------|
| [Produção — visão geral](./producao/README.md) | Lista de peças, corte, etiquetas, E.1–E.4 | ✅ |
| [Lista de peças e furos](./producao/01-lista-de-pecas.md) | Dobradiça, minifix | ✅ |
| [Plano de corte MaxRects](./producao/02-plano-de-corte.md) | Nesting, CSV | ✅ |
| [Etiquetas PDF](./producao/03-etiquetas-pdf.md) | Etiquetas por peça | ✅ |
| [Export .tap Jaraguá Mach4 (E.4)](./producao/07-export-tap-jaragua-mach4.md) | G-code Solid TAF / Mach4 | ✅ E.4 |

### Detalhamento técnico

| Artigo | Conteúdo | Status Traços |
|--------|----------|----------------|
| [Detalhamento — visão geral](./detalhamento/README.md) | PDF técnico, planta, DXF | ✅ |
| [PDF técnico e planta](./detalhamento/01-pdf-tecnico-planta.md) | Elevações, cotas, furos | ✅ |
| [Exportação DXF](./detalhamento/02-exportacao-dxf.md) | Planta e peças com furos | ✅ |

### Escala e distribuição

| Artigo | Conteúdo | Status Traços |
|--------|----------|----------------|
| [Escala — visão geral](./escala/README.md) | Instalador, biblioteca, backup | ✅ |
| [Distribuição local](./escala/01-distribuicao-local.md) | Instalador, ERP JSON, publish.ps1 | ✅ |

### Índice rápido

- [Guia de início rápido](./GUIA-INICIO-RAPIDO.md) — roteiro completo do zero ao corte
- [Índice rápido](./INDICE-RAPIDO.md) — mapa de artigos, fixtures e screenshots

---

## Para quem mantém o manual

Leia [COMO-MANTER.md](./COMO-MANTER.md) — fluxo de screenshots, GIFs e atualização por feature. Trilha de produto: [ESCOPO-V3-PROMOB-COMPLETO.md](../ESCOPO-V3-PROMOB-COMPLETO.md).

---

## Instalador e versão

Cada build de distribuição registra data e hora:

- Barra de status: `Build: dd/MM/yyyy HH:mm`
- Arquivo `dist/last-build.txt` após `installer\publish.ps1`
- Programas e Recursos: `Traços 3D Studio (build dd/MM/yyyy HH:mm)`
