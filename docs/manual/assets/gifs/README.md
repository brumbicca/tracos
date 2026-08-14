# GIFs do manual

Animações curtas geradas a partir de sequências de screenshots de aceite.

| Arquivo | Conteúdo | Frames |
|---------|----------|--------|
| `paredes-construir-horario.gif` | Construção quadrado horário | horario/pre → 5000 → fechado → planta |
| `paredes-cota-manual.gif` | Cotas reta e angular no editor | ferramenta → reta → angular |
| `paredes-mover-particao.gif` | Arraste parede móvel | antes → durante → após |
| `paredes-encontro-editor.gif` | Sample partição + editor/encontro | sample → encontro T → editor |

## Regenerar GIFs

```powershell
python installer/scripts/build-manual-gifs.py
```

(ou ver sequências em [COMO-MANTER.md](../COMO-MANTER.md))

## Referência no markdown

```markdown
![Mover partição](../assets/gifs/paredes-mover-particao.gif)
```
