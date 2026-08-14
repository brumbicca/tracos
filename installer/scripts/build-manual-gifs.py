"""Gera GIFs do manual a partir de screenshots em docs/screenshots/parede/."""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SHOTS = ROOT / "docs/screenshots/parede"
OUT = ROOT / "docs/manual/assets/gifs"

SEQUENCES = {
    "paredes-mover-particao.gif": [
        "movimentar-g4/planta-antes-arrastar.png",
        "movimentar-g4/durante-arraste-linha-azul.png",
        "movimentar-g4/apos-arraste-planta-v2.png",
    ],
    "paredes-construir-horario.gif": [
        "horario/fase-parede-horario-pre.png",
        "horario/fase-parede-horario-5000.png",
        "horario/fase-parede-horario-fechado.png",
        "horario/fase-parede-horario-planta.png",
    ],
    "paredes-encontro-editor.gif": [
        "encontro-g1/sample-carregado-planta.png",
        "encontro-g1/encontro-t-aplicado.png",
        "editor-p4/editor-ativado-quadrado-5000.png",
    ],
    "paredes-cota-manual.gif": [
        "cotas-manuais-m5/cota-reta-ferramenta-ativada.png",
        "cotas-manuais-m5/cota-reta-5000-criada.png",
        "cotas-manuais-m5/cota-angular-90-criada.png",
    ],
}


def make_gif(name: str, rel_paths: list[str], duration_ms: int = 900) -> None:
    paths = [SHOTS / p for p in rel_paths]
    for p in paths:
        if not p.exists():
            raise FileNotFoundError(p)

    imgs = [Image.open(p).convert("RGB") for p in paths]
    w = min(i.width for i in imgs)
    h = min(i.height for i in imgs)
    resized = []
    for img in imgs:
        ratio = min(w / img.width, h / img.height)
        nw, nh = int(img.width * ratio), int(img.height * ratio)
        resized.append(img.resize((nw, nh), Image.Resampling.LANCZOS))

    OUT.mkdir(parents=True, exist_ok=True)
    out_path = OUT / name
    resized[0].save(
        out_path,
        save_all=True,
        append_images=resized[1:],
        duration=duration_ms,
        loop=0,
        optimize=True,
    )
    print(f"OK {out_path.name} ({len(resized)} frames)")


if __name__ == "__main__":
    for gif_name, frames in SEQUENCES.items():
        make_gif(gif_name, frames)
