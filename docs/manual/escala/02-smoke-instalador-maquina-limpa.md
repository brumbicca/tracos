# Smoke do instalador — máquina limpa

**Última revisão:** 02/07/2026  
**Build alvo:** conferir `dist/last-build.txt` (ex.: `2026.07.02.2327`)

**Pacote smoke atual:** `dist/Tracos3DStudio-smoke-pack-2026.07.02.2327.zip`

Validação **fora do ambiente de desenvolvimento**: PC ou VM **sem** Visual Studio, **sem** .NET SDK e **sem** pasta do repositório Traços.

Relacionado: [Distribuição local](./01-distribuicao-local.md) · [PLANO-EXECUCAO.md](../../PLANO-EXECUCAO.md) (item 3 pós-beta)

---

## Objetivo

Confirmar que o instalador oficial entrega um Traços **executável de ponta a ponta** para um usuário final:

1. Instala sem erro.
2. Abre com build correto na barra de status.
3. Cria/edita projeto, salva `.tracos` e gera saída comercial mínima (orçamento/PDF).

---

## Preparação

### Máquina de teste

| Requisito | Detalhe |
|-----------|---------|
| SO | Windows 10/11 **64 bits** |
| Ambiente | **Sem** .NET SDK / Visual Studio instalados |
| Rede | Opcional (app é offline) |
| Antivírus | Anotar se bloqueou o `.exe` durante o teste |

> **VM recomendada:** snapshot antes do teste; restaurar entre builds.

### Arquivos para levar (pen drive ou pasta compartilhada)

**Pacote pronto (recomendado):** `dist/Tracos3DStudio-smoke-pack-<versão>.zip` — contém setup, `last-build.txt`, `fase-2-cozinha-L.tracos` e este checklist (`02-smoke-instalador-maquina-limpa.md`).

**Gerar pacote (desenvolvedores):** após `installer\publish.ps1`, execute `powershell -ExecutionPolicy Bypass -File installer\smoke-pack.ps1`.

| Arquivo | Obrigatório | Uso |
|---------|-------------|-----|
| `dist/Tracos3DStudio-setup.exe` | ✅ | Instalação |
| `dist/last-build.txt` | ✅ | Conferir versão esperada |
| `fase-2-cozinha-L.tracos` | Opcional | Atalho no smoke **B** (fluxo completo sem desenhar) |

Copie do repositório **antes** de ir à máquina limpa — lá não haverá `samples/` nem `publish/`.

### Registro do teste

Preencha ao final:

| Campo | Valor |
|-------|-------|
| Data | |
| Build (`last-build.txt`) | |
| SO / RAM | |
| Executado por | |
| Resultado geral | ☐ Aprovado ☐ Reprovado |

---

## Parte A — Instalação (≈ 5 min)

| # | Passo | OK? | Observação |
|---|-------|-----|------------|
| A1 | Executar `Tracos3DStudio-setup.exe` como usuário normal | ☐ | |
| A2 | Assistente Inno Setup conclui **sem** erro | ☐ | |
| A3 | Atalho **Traços 3D Studio** aparece no Menu Iniciar | ☐ | |
| A4 | **Programas e Recursos** lista *Traços 3D Studio* com build legível | ☐ | |
| A5 | Primeira abertura **não** pede instalar .NET manualmente | ☐ | Self-contained |

**Screenshot:** `docs/screenshots/aceite-e2e/release-smoke-01-instalador-concluido.png` — Menu Iniciar ou Programs & Features.

---

## Parte B — Smoke funcional mínimo (≈ 15–20 min)

Fluxo **do zero**, sem fixture — prova desenho + persistência + comercial.

| # | Passo | Critério de sucesso | OK? |
|---|-------|---------------------|-----|
| B1 | Abrir o Traços pelo Menu Iniciar | Janela principal; toolbar visível | ☐ |
| B2 | Barra de status | **Build:** bate com `last-build.txt` · **Unidade: mm** | ☐ |
| B3 | **Novo projeto** | Projeto em branco; título com asterisco ao alterar | ☐ |
| B4 | Desenhar ambiente fechado | **Parede** → 4 segmentos → ambiente fechado; piso visível | ☐ |
| B5 | Inserir **Porta** | Clique na face; painel Largura/Altura coerentes | ☐ |
| B6 | Inserir 1 módulo (ex.: **Balcão 2 Portas**) | Módulo visível no viewport | ☐ |
| B7 | **Projeto → Dados do cliente e da obra...** | Preencher Nome + Obra; **OK** sem crash | ☐ |
| B8 | **Orçamento → Abrir orçamento...** | Janela abre; dados do cliente aparecem; tabela com módulo | ☐ |
| B9 | **Exportar PDF...** (orçamento) | Arquivo `.pdf` gerado e abre no leitor | ☐ |
| B10 | **Salvar projeto** | Salvar `.tracos` em Documentos; fechar e **Abrir** de novo | ☐ |
| B11 | Geometria após reabrir | Paredes, porta e módulo idênticos | ☐ |

**Screenshots sugeridos:**

| Arquivo | Conteúdo |
|---------|----------|
| `release-smoke-02-app-build.png` | Barra de status com Build |
| `release-smoke-03-ambiente-fechado.png` | Viewport com ambiente + porta |
| `release-smoke-04-dados-cliente.png` | Janela Dados do cliente e da obra |
| `release-smoke-05-orcamento.png` | Janela de orçamento |
| `release-smoke-06-projeto-reaberto.png` | Projeto reaberto após salvar |

---

## Parte C — Smoke estendido (opcional, ≈ 10 min)

Use se quiser validar o **pacote completo** sem redesenhar (copie `fase-2-cozinha-L.tracos`).

| # | Passo | OK? |
|---|-------|-----|
| C1 | **Abrir projeto** → `fase-2-cozinha-L.tracos` | ☐ |
| C2 | Status: **Ambiente: Fechado** · módulos visíveis | ☐ |
| C3 | **Projeto → Lista de peças...** abre sem exceção | ☐ |
| C4 | **Produção → Plano de corte...** abre com chapas | ☐ |
| C5 | **Projeto → Exportar PDF técnico...** gera arquivo | ☐ |
| C6 | **Ferramentas → Backup do projeto...** gera `.zip` | ☐ |

**Screenshot:** `release-smoke-07-cozinha-L-planta.png` (vista Planta).

---

## Parte D — Desinstalação (opcional)

| # | Passo | OK? |
|---|-------|-----|
| D1 | Desinstalar pelo Painel de Controle | ☐ |
| D2 | Pasta de instalação removida (ou só atalhos, conforme Inno) | ☐ |
| D3 | Reinstalar mesma versão → app abre normalmente | ☐ |

---

## Critérios de aprovação

**Aprovado** se:

- Parte **A** — 100% OK.
- Parte **B** — todos os itens B1–B11 OK (B8 pode pedir *Continuar* na auditoria se projeto mínimo; **não** deve crashar).
- Nenhum bloqueio permanente de antivírus sem workaround documentado.

**Reprovado** se:

- Instalador falha ou app não abre sem SDK.
- Crash ao abrir **Dados do cliente**, orçamento ou salvar `.tracos`.
- Build na barra de status diferente do `last-build.txt` do pacote testado.

---

## Após o smoke

1. Copiar screenshots para `docs/screenshots/aceite-e2e/` no repositório.
2. Atualizar a linha de smoke em [GUIA-INICIO-RAPIDO.md](../GUIA-INICIO-RAPIDO.md) com build e data.
3. Marcar item **3** em [PLANO-EXECUCAO.md](../../PLANO-EXECUCAO.md) como ✅ (com data e build).
4. Se reprovado: abrir issue com SO, build, passo que falhou e screenshot.

---

## Próxima trilha (V3)

**V1+V2 ✅** 26/06/2026 — [ESCOPO-V1-VS-PROMOB.md](../../ESCOPO-V1-VS-PROMOB.md).

**Trilha ativa:** [ESCOPO-V3-PROMOB-COMPLETO.md](../../ESCOPO-V3-PROMOB-COMPLETO.md).

| Item | Gate |
|------|------|
| Smoke Parte A (VM limpa) | **V3.6b** |
| Nuvem / Connect / ERP live | **V3.5** |
