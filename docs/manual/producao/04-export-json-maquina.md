# Traços 3D — Export JSON para máquina (E.1)

**Última revisão:** 19/06/2026  
**Referência Promob:** export `.planner` → Cut Pro / chão de fábrica

---

## Neste artigo

- Exportar plano de corte em **JSON** estruturado para integração com CNC/ERP
- Conteúdo: chapas, posições, fita de borda e **furos** por peça
- Diferença entre CSV, JSON máquina e pacote ERP

---

## Quando usar

| Formato | Uso |
|---------|-----|
| **CSV** | Otimizadores genéricos, planilhas |
| **JSON máquina** (`tracos-cut-plan`) | Integração programática, scripts de fábrica, conversores CNC |
| **Pacote ERP** (Projeto → Exportar pacote ERP) | Orçamento + peças + corte completo para back-office |

O JSON máquina é focado em **produção**: nesting MaxRects + furos (dobradiça, minifix) em coordenadas **locais da peça**.

---

## Exportar

1. Abra um projeto com módulos (ex.: `fase-2-cozinha-L.tracos`).
2. Menu **Produção → Exportar JSON plano de corte (máquina)...**
3. Salve como `{projeto}-plano-corte-maquina.json`.

Confirmação: número de chapas, peças e aproveitamento médio.

---

## Estrutura do arquivo

```json
{
  "schemaVersion": 1,
  "format": "tracos-cut-plan",
  "project": { "name": "...", "clientName": "..." },
  "settings": {
    "sheetLengthMm": 2750,
    "sheetWidthMm": 1850,
    "cutKerfMm": 3,
    "algorithm": "MaxRects"
  },
  "summary": {
    "totalSheets": 2,
    "totalPlacedPieces": 48,
    "overallUtilizationPercent": 71.2
  },
  "sheets": [
    {
      "index": 1,
      "materialName": "MDF Branco",
      "pieces": [
        {
          "instanceId": 1,
          "moduleName": "Balcão 2 Portas",
          "pieceName": "Lateral esquerda",
          "sheetXmm": 10,
          "sheetYmm": 10,
          "lengthMm": 800,
          "widthMm": 550,
          "rotated": false,
          "edgeBand": "Frente",
          "holes": [
            { "kind": "hingeCup", "edge": "left", "posXmm": 22, "posYmm": 100, "diameterMm": 35, "depthMm": 13 }
          ]
        }
      ]
    }
  ]
}
```

**Nota:** `holes` usam o sistema de coordenadas da **peça**, não da chapa. Se a peça foi rotacionada no nesting, aplique a rotação no conversor CNC.

---

## Fixture e amostra

| Artefato | Uso |
|----------|-----|
| `fase-2-cozinha-L.tracos` | 4 módulos → dezenas de peças com furos |
| `docs/screenshots/producao/fase-E.1-amostra-plano-corte-maquina.json` | Amostra JSON `tracos-cut-plan` |
| `docs/screenshots/producao/fase-E.1-menu-export-maquina.png` | Aceite E.1 — menu Produção |

---

## Voltar ao índice

[Produção — visão geral](./README.md)
