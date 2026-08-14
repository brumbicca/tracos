# Screenshots de testes visuais — Traços 3D Studio

Capturas geradas via WinApp MCP ou manualmente durante validação de features.

**Manual de uso:** screenshots referenciados em `docs/manual/paredes/` — ver [manual/README.md](../manual/README.md).

## Estrutura

| Pasta | Conteúdo |
|-------|----------|
| `fase-1/` | Ambiente, vistas, persistência (1.3–1.5) |
| `fase-2/` | Módulos, cozinha em L, colisão |
| `modulos/` | Face interna, cotas, cozinha L, colisão, dormitório, aba Ambiente B.2 | `inserir-balcao-*.png`, `dormitorio-planta.png`, `colisao-modulos-planta.png`, `fase-B.2-aba-ambiente.png` |
| `modulos/V3.7-spike/` | Spike engenharia modulação — Promob Plus ao vivo | `promob-construir-armario-modo.png`, `promob-modulo-selecionado-geral.png` |
| `modulos/V3.7b/` | Editor de modulação V3.7b | `fase-V3.7b-editor-modulacao-aberto.png` |
| `materiais/` | Janela Materiais (C.1), drag no viewport (C.2) | `janela-materiais.png` |
| `aberturas/` | Porta, janela, fixture porta+janela |
| `orcamento/` | Auditoria, janela orçamento | `auditoria-pre-orcamento.png`, `janela-orcamento.png` |
| `producao/` | Lista peças, plano MaxRects | `lista-pecas-furos.png`, `plano-corte-maxrects.png` |
| `docs/screenshots/aceite-e2e/` | Aceite ponta a ponta + trilha Promob A.1/A.2 | `fase-regiao-poligono-frontal.png`, `fase-camadas-perspectiva.png` |
| `fase-3/` | Legado (referência) | migrado para `orcamento/` |
| `fase-5/` | Legado (referência) | migrado para `producao/` |
| `fase-6/` | Biblioteca, nesting MaxRects |
| `parede/` | Paridade Promob — paredes (subpastas por marco) |
| `debug/` | Depuração temporária (`debug-viewport-*`, `temp-*`) |
| `exploratorio/` | Rascunhos de automação (`ss-*`, `screenshot-*`) |

## Parede (`parede/`)

| Subpasta | Marco | Exemplos |
|----------|-------|----------|
| `horario/` | Desenho horário / validação inicial | `fase-parede-horario-*.png` |
| `orientacao/` | Painel Orientação M1–M3 | `fase-parede-orientacao-*.png` |
| `selecao-face-grupo/` | Face vs grupo (topo) | `fase-parede-selecao-*.png` |
| `cotas-automaticas-m4/` | Cotas automáticas nos vértices | `fase-parede-cotas-automaticas*.png` |
| `quadrado-5000/` | Aceite visual 4×5000 mm | `fase-parede-quadrado-5000-*.png` |
| `referencia-m6/` | Construção com referência | `fase-parede-referencia-*.png` |
| `movimentar-g4/` | Movimentar parede (G4) | `fase-parede-movimentar-*.png` |
| `editor-p4/` | Editor de Paredes (vista 2D dedicada) | `editor-ativado-*.png` |
| `cotas-manuais-m5/` | Cotas manuais retas/angulares (M5) | `cota-reta-*.png` |
| `segmentar-g3/` | Segmentar parede (G3) | `segmentar-*.png` |
| `304050-m7/` | Ferramenta 30-40-50 (M7) | `304050-*.png` |
| `chanfro-g5/` | Aparar Parede / chanfro manual (G5) | `chanfro-*.png` |
| `curvas-g2/` | Paredes curvas (G2) | `curva-*.png` |
| `drywall-g6/` | Tipo Dry Wall (G6) | `drywall-*.png` |
| `encontro-g1/` | Encontro Canto / T (G1) | `encontro-*.png` |
| `camadas-faixas/` | Camadas, faixas e regiões | `faixas-regioes-*.png`, `janela-camadas.png`, `faixa-regiao-material-frontal.png`, `fase-A.6a-remover-camada-vazia.png` |
| `promob-comparacao/` | Comparação lado a lado Promob | `teste-winapp-promob*.png` |
| `validacao/` | Outros testes pontuais | `fase-parede-validacao-*.png` |

## Convenção de nomes (novos arquivos)

Salvar em **`docs/screenshots/<fase ou parede>/<subpasta>/`** com prefixo descritivo:

```
docs/screenshots/parede/referencia-m6/planta-linha-azul.png
docs/screenshots/parede/movimentar-g4/planta-apos-arrastar.png
docs/screenshots/fase-2/fase-2.3-modulo-parede.png
```

**Não** salvar novos PNG na raiz do repositório.

## Fixtures de teste (`.tracos`)

| Arquivo | Uso |
|---------|-----|
| `samples/quadrado-5000-horario.tracos` | Aceite M1–M4, cotas automáticas |
| `samples/quadrado-5000-particao-movel.tracos` | G4 movimentar parede (partição `IsMovable`) |

```powershell
dotnet run --project Tracos3DStudio -- samples/quadrado-5000-particao-movel.tracos
```
