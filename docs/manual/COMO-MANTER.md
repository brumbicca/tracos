# Como manter o manual atualizado

Guia para o time (e para o agente de IA) alinado às regras de teste visual do projeto.

---

## Princípio

**Um artigo = um fluxo de usuário**, como na Central Promob. Quando uma feature de UI/viewport entra no código:

1. Atualizar o artigo correspondente (ou criar um novo).
2. Adicionar screenshot(s) em `docs/screenshots/`.
3. Marcar **Última revisão** no topo do artigo.
4. Se o checklist Promob mudou, atualizar [PLANO-EXECUCAO.md](../PLANO-EXECUCAO.md) e [ESCOPO-V3-PROMOB-COMPLETO.md](../ESCOPO-V3-PROMOB-COMPLETO.md) (trilha ativa).

---

## Imagens estáticas (PNG)

| Tipo | Onde salvar | Nome |
|------|-------------|------|
| Aceite de feature | `docs/screenshots/parede/<marco>/` | `descricao-clara.png` |
| Manual (reuso) | Mesma pasta ou copiar referência | Link relativo no markdown |

**No artigo**, use caminho relativo a partir de `docs/manual/`:

```markdown
![Planta com cotas](../../screenshots/parede/quadrado-5000/fase-parede-quadrado-5000-planta.png)
```

Convenção de pastas: ver [screenshots/README.md](../screenshots/README.md).

---

## GIFs animados

Pasta: `docs/manual/assets/gifs/` — ver índice em [assets/gifs/README.md](./assets/gifs/README.md).

Regenerar após novos screenshots:

```powershell
python installer/scripts/build-manual-gifs.py
```

| GIF | Fluxo |
|-----|--------|
| `paredes-construir-horario.gif` | Desenho quadrado sentido horário |
| `paredes-cota-manual.gif` | Cota reta e angular |
| `paredes-mover-particao.gif` | Arraste parede móvel |
| `paredes-encontro-editor.gif` | Sample partição + editor/encontro |

---

## Estrutura de um artigo (modelo Promob)

1. Título claro (`# Traços 3D — …`)
2. Bloco **Neste artigo** (bullets)
3. **IMPORTANTE** quando há regra de medida ou sentido horário
4. Passos numerados com botões em negrito
5. Figura após cada bloco importante
6. Tabela **Promob vs Traços** só quando ajuda o usuário (não duplicar o plano inteiro)
7. **Última revisão** + link ao artigo Promob equivalente

---

## Samples para captura

| Arquivo | Uso no manual |
|---------|----------------|
| `samples/quadrado-5000-horario.tracos` | Construção, orientação, cotas automáticas |
| `samples/quadrado-5000-particao-movel.tracos` | Movimentar, segmentar, encontro T |

```powershell
dotnet run --project Tracos3DStudio -- samples/quadrado-5000-horario.tracos
```

---

## Checklist rápido antes de publicar artigo

- [ ] Passos batem com `AutomationId` / texto dos botões em `MainWindow.xaml`
- [ ] Screenshot ou GIF incluído
- [ ] Data no topo do artigo
- [ ] Link no [índice de paredes](./paredes/README.md) se for novo tópico
