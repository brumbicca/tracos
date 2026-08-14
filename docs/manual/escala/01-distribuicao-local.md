# Traços 3D — Distribuição e ferramentas

**Última revisão:** 20/06/2026

---

## Neste artigo

- Instalador Windows
- Biblioteca própria, backup e ERP JSON
- Regenerar build de distribuição

---

## Instalador Windows

O instalador oficial fica em:

- `dist/Tracos3DStudio-setup.exe`
- Versão registrada em `dist/last-build.txt`

Após instalar, o Traços aparece no menu Iniciar. A barra de status mostra **Build:** com data/hora do pacote.

**Build atual de referência:** ver `dist/last-build.txt` (ex.: `2026.06.26.2012`).

---

## Ferramentas (menu Ferramentas)

| Ação | Uso |
|------|-----|
| **Gerenciar biblioteca...** | Módulos personalizados e overrides (`.tracos-lib`) |
| **Recarregar biblioteca** | Relê o JSON local e atualiza aba **Inserir** sem reiniciar o app |
| **Exportar pacote ERP...** | JSON com módulos, peças e totais para integração |
| **Backup do projeto...** | ZIP com projeto + biblioteca local |

---

## Perfis de construção

**Projeto → Perfil de construção** — espessura padrão do painel:

- Padrão (18 mm)
- Reforçado (25 mm)
- Econômico (15 mm)

---

## Regenerar instalador (desenvolvedores)

No PowerShell, na raiz do repositório:

```powershell
Stop-Process -Name Tracos3DStudio -Force -ErrorAction SilentlyContinue
powershell -ExecutionPolicy Bypass -File installer\publish.ps1
```

Requisitos: .NET SDK, Inno Setup 6.

**Smoke pack (máquina limpa):** após o publish, empacote com:

```powershell
powershell -ExecutionPolicy Bypass -File installer\smoke-pack.ps1
```

Gera `dist/Tracos3DStudio-smoke-pack-<versão>.zip` (setup + `last-build.txt` + fixture + checklist).

**Smoke pós-build:** [02-smoke-instalador-maquina-limpa.md](../escala/02-smoke-instalador-maquina-limpa.md) — validar o `.exe` em PC sem SDK.

---

## Futuro (não no MVP local)

- Multi-usuário / projetos em nuvem
- API REST ERP em tempo real
