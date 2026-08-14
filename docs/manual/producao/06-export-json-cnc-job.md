# Traços 3D — JSON CNC tracos-cnc-job (E.3)

**Última revisão:** 19/06/2026  
**Referência Promob:** export `.planner` → Cut Pro / post-processador CNC

---

## Neste artigo

- Formato **`tracos-cnc-job`** — job CNC unificado para post-processadores
- Operações de **corte** (contorno retangular na chapa) e **furo** (coords absolutas)
- Diferença entre E.1, E.2 e E.3

---

## Quando usar

| Formato | Conteúdo |
|---------|----------|
| **JSON máquina (E.1)** | Plano completo; furos em coords **locais** da peça |
| **CSV furos CNC (E.2)** | Só furos, uma linha por furo |
| **JSON CNC (E.3)** | **Cortes + furos** em coords **de chapa**, pronto para scripts/conversores |

Use o `tracos-cnc-job` quando o post-processador precisa de um único arquivo com todas as operações CNC (similar ao pacote Cut Pro).

---

## Exportar

| Onde | Ação |
|------|------|
| **Produção → Exportar JSON CNC (tracos-cnc-job)...** | Diálogo de arquivo |
| Janela **Plano de corte** → **Exportar JSON CNC...** | Mesmo formato |

---

## Estrutura

```json
{
  "schemaVersion": 1,
  "format": "tracos-cnc-job",
  "units": "mm",
  "coordinateSystem": "sheet-origin-bottom-left",
  "sheets": [
    {
      "index": 1,
      "materialName": "MDF Branco",
      "thicknessMm": 18,
      "operations": [
        {
          "type": "cut",
          "instanceId": 1,
          "pieceName": "Lateral esquerda",
          "contourMm": [[10, 10], [810, 10], [810, 560], [10, 560]]
        },
        {
          "type": "drill",
          "instanceId": 1,
          "sheetXmm": 32,
          "sheetYmm": 110,
          "diameterMm": 35,
          "depthMm": 13,
          "kind": "hingeCup"
        }
      ]
    }
  ]
}
```

- **`cut`** — retângulo fechado (4 vértices) na chapa, após nesting
- **`drill`** — coordenadas `sheetXmm` / `sheetYmm` (rotação 90° já aplicada quando a peça foi rotacionada)

---

## Artefatos de aceite

| Arquivo | Conteúdo |
|---------|----------|
| `docs/screenshots/producao/fase-E.3-menu-export-cnc-job.png` | Menu Produção |
| `docs/screenshots/producao/fase-E.3-amostra-cnc-job.json` | Amostra cozinha L |

---

## Voltar ao índice

[Produção — visão geral](./README.md)
