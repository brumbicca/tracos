# Traços 3D — Exportação DXF

**Última revisão:** 20/06/2026

---

## Neste artigo

- **DXF planta** — paredes e módulos
- **DXF peças** — contornos e furos para CAD/CNC
- **Importar DXF planta**

---

## DXF planta

1. **Projeto → Exportar DXF planta...**
2. Camadas no arquivo:
   - `PAREDES` — linhas das paredes
   - `MODULOS` — contorno dos módulos na planta

Compatível com importação de volta via **Projeto → Importar DXF planta...**

---

## DXF peças

1. **Projeto → Exportar DXF peças...** (ou botão na **Lista de peças**).
2. Camadas:
   - `PECAS` — retângulos de cada peça (layout em linhas)
   - `FUROS` — círculos (dobradiça Ø35, minifix Ø5/Ø15)

Cada instância de peça é desenhada com espaçamento de 80 mm. Furos usam as mesmas coordenadas da lista de peças.

Fixture de aceite: `docs/screenshots/detalhamento/pecas-cozinha-L.dxf` (gerado pelos testes).

---

## Importar planta

**Projeto → Importar DXF planta...** — lê entidades `LINE` e reconstrói paredes no ambiente (útil para planta base de arquiteto).
