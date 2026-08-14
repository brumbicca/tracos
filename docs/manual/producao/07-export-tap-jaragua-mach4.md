# Traços 3D — Export .tap Jaraguá Mach4 (E.4)

**Última revisão:** 26/06/2026  
**Referência:** post `JRGCNC - TAF.pp` · router **Solid TAF** Jaraguá CNC · Mach4

---

## Neste artigo

- Export nativo **G-code `.tap`** para Mach4 (Jaraguá)
- Entrada: plano de corte Traços (`tracos-cnc-job` internamente)
- Operações: **contorno** das peças na chapa + **furos** (vertical e horizontal minifix)

---

## Gate E.4 (fechado 26/06/2026)

| Critério | Valor |
|----------|--------|
| Máquina | Router **Solid TAF** — Jaraguá CNC + **Mach4** |
| Amostra executada | `teste corte.tap` + validação na chapa |
| Validador | Operador na Solid TAF |
| Formato | `.tap` G-code (post JRG CNC SOLID TAF) |
| Escopo v1 | Cozinha L → contorno + furos, ferramenta **T3**, Z corte **-0,1 mm** |

---

## Exportar

| Onde | Ação |
|------|------|
| **Produção → Exportar .tap Jaraguá (Mach4)...** | Diálogo de arquivo `.tap` |
| Janela **Plano de corte** → **Exportar .tap Jaraguá...** | Mesmo formato |

Com **várias chapas**, o Traços grava `nome-chapa-01.tap`, `nome-chapa-02.tap`, … na mesma pasta.

---

## Parâmetros padrão (calibrados com `teste corte.tap`)

| Parâmetro | Valor |
|-----------|--------|
| Ferramenta | T3 |
| RPM | 18000 |
| Raio fresa contorno | 3,5 mm |
| Offset origem X | 9,5 mm |
| Z seguro | 23,080 mm |
| Z corte | -0,100 mm |
| Furo horizontal minifix | Z = 9 mm (chapa 18 mm) |

---

## Fluxo recomendado

1. Abrir projeto e gerar **Plano de corte** (MaxRects).
2. **Produção → Exportar .tap Jaraguá (Mach4)...**
3. Carregar o `.tap` no **Mach4** e validar na chapa.

O JSON `tracos-cnc-job` (E.3) continua disponível para outros post-processadores.

---

## Artefatos de aceite

| Arquivo | Conteúdo |
|---------|----------|
| `docs/screenshots/producao/fase-E.4-amostra-cozinha-chapa-01.tap` | Amostra cozinha L, chapa 1 |
| `docs/screenshots/producao/fase-E.4-mcp-aceite-chapa-01.tap` | Aceite MCP — export cozinha L chapa 1 |
| `docs/screenshots/producao/fase-E.4-menu-export-tap-jaragua.png` | Menu Produção |
| `docs/screenshots/producao/fase-E.4-janela-plano-corte-export-tap.png` | Janela plano de corte |
| `teste corte.tap` (referência fábrica) | G-code executado na Solid TAF |

---

## Voltar ao índice

[Produção — visão geral](./README.md)
