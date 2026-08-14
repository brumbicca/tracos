# Traços 3D — Escala e distribuição

**Última revisão:** 01/07/2026 (V1+V2 local ✅ · V3.5 plataforma ⬜)

**Marcos:** [ESCOPO-V1-VS-PROMOB.md](../../ESCOPO-V1-VS-PROMOB.md) · **Trilha V3:** [ESCOPO-V3-PROMOB-COMPLETO.md](../../ESCOPO-V3-PROMOB-COMPLETO.md)  
**Build:** `2026.06.26.2012`

---

## Índice

| Artigo | Conteúdo | Status |
|--------|----------|--------|
| [Distribuição local](./01-distribuicao-local.md) | Instalador, biblioteca, backup, ERP JSON | ✅ |
| [Smoke instalador — máquina limpa](./02-smoke-instalador-maquina-limpa.md) | Checklist pós-release (PC/VM sem SDK) | 🟡 Parte A · gate **V3.6b** |

**Instalador:** `dist/Tracos3DStudio-setup.exe`  
**Registro de build:** `dist/last-build.txt`

---

## Escopo local entregue (✅)

- Instalador Inno Setup (win-x64, self-contained)
- Editor de biblioteca + import/export catálogo (`.tracos-lib`)
- Backup ZIP e export JSON ERP
- Perfis de construção (15 / 18 / 25 mm)
- Recarregar biblioteca sem reiniciar (L10)

## Backlog V3 — plataforma (⬜)

Ver [ESCOPO-V3 §V3.5](../../ESCOPO-V3-PROMOB-COMPLETO.md#v35--plataforma--escala-fase-6-completa):

- Nuvem multi-usuário / Connect-like (L9)
- API REST ERP em tempo real
- Sync catálogo corporativo centralizado
