# Traços 3D — CSV furos CNC (E.2)

**Última revisão:** 19/06/2026  
**Referência Promob:** usinagem / Cut Pro — coordenadas na chapa

---

## Neste artigo

- Exportar **furos** (dobradiça, minifix) com coordenadas **na chapa** para scripts CNC
- Conversão automática quando a peça foi **rotacionada** no nesting
- Diferença entre CSV plano de corte, JSON máquina e CSV furos CNC

---

## Quando usar

| Formato | Conteúdo |
|---------|----------|
| **CSV plano de corte** | Posição das **peças** na chapa |
| **JSON máquina (E.1)** | Plano completo + furos em coords **locais** da peça |
| **CSV furos CNC (E.2)** | Uma linha por **furo** com `ChapaX_mm` / `ChapaY_mm` prontos para máquina |

Use o CSV furos quando o post-processador CNC precisa de coordenadas absolutas na chapa (similar ao fluxo Cut Pro).

---

## Exportar

| Caminho | Uso |
|---------|-----|
| **Produção → Exportar CSV furos CNC...** | Diálogo de arquivo direto |
| Janela **Plano de corte** → **Exportar CSV furos CNC...** | Mesmo formato após recalcular nesting |

Fixture: `fase-2-cozinha-L.tracos` (portas com furos de dobradiça).

---

## Colunas do CSV

```
Chapa;Material;Espessura_mm;Instancia;Modulo;Peca;PecaX_mm;PecaY_mm;PecaL_mm;PecaA_mm;Rotacionada;
FuroTipo;Aresta;LocalX_mm;LocalY_mm;ChapaX_mm;ChapaY_mm;Diametro_mm;Profundidade_mm
```

- **LocalX/Y** — coordenadas na peça (antes do nesting)
- **ChapaX/Y** — coordenadas absolutas na chapa (após rotação, se houver)
- **FuroTipo** — `HingeCup`, `MinifixDowel`, `MinifixCam`

---

## Rotação no nesting

Se `Rotacionada = Sim`, o Traços aplica rotação 90° horária:

- `ChapaX = PecaX + LocalY`
- `ChapaY = PecaY + (comprimento original − LocalX)`

---

## Artefatos de aceite

| Arquivo | Uso |
|---------|-----|
| `docs/screenshots/producao/fase-E.2-menu-furos-cnc.png` | Menu Produção |
| `docs/screenshots/producao/fase-E.2-furos-cnc.csv` | Amostra cozinha L |

---

## Voltar ao índice

[Produção — visão geral](./README.md)
