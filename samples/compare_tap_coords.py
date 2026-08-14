import re
from pathlib import Path

def parse_cut_segments(path):
    lines = Path(path).read_text(encoding="utf-8", errors="ignore").splitlines()
    segments = []
    current = []
    for line in lines:
        m = re.search(r"N\d+(G0\d?|G1|G2|G3)(.+)", line.replace(" ", ""))
        if not m:
            continue
        cmd, rest = m.group(1), m.group(2)
        x = re.search(r"X([+-]?[\d.]+)", rest)
        y = re.search(r"Y([+-]?[\d.]+)", rest)
        z = re.search(r"Z([+-]?[\d.]+)", rest)
        if not (x and y):
            continue
        pt = (float(x.group(1)), float(y.group(1)), float(z.group(1)) if z else None)
        if cmd.startswith("G00"):
            if current:
                segments.append(current)
            current = []
        elif pt[2] is not None and pt[2] <= 0:
            current.append(pt)
    if current:
        segments.append(current)
    return segments

def bbox(seg):
    xs = [p[0] for p in seg]
    ys = [p[1] for p in seg]
    return {
        "minx": min(xs),
        "miny": min(ys),
        "maxx": max(xs),
        "maxy": max(ys),
        "w": max(xs) - min(xs),
        "h": max(ys) - min(ys),
    }

def apply_offset(b, ox, oy):
    return {**b, "minx": b["minx"] + ox, "miny": b["miny"] + oy, "maxx": b["maxx"] + ox, "maxy": b["maxy"] + oy}

def norm_size(b):
    return tuple(sorted((round(b["w"], 0), round(b["h"], 0))))

def size_close(a, b, tol=6):
    sa, sb = norm_size(a), norm_size(b)
    return abs(sa[0] - sb[0]) <= tol and abs(sa[1] - sb[1]) <= tol

def pos_close(a, b, tol=8):
    return abs(a["minx"] - b["minx"]) <= tol and abs(a["miny"] - b["miny"]) <= tol

MIN_DIM = 200
OFFSET = (23.0, 10.0)

tr_path = Path(r"c:\Users\brumb\Downloads\Tracos3DStudio\samples\fase-2-cozinha-L-chapa-01.tap")
asp_path = Path(r"c:\Users\brumb\Downloads\Tracos3DStudio\TAP ASPIRE COMPARAÇÃO.tap")

tr = [bbox(s) for s in parse_cut_segments(tr_path) if bbox(s)["w"] >= MIN_DIM and bbox(s)["h"] >= MIN_DIM]
asp_raw = [bbox(s) for s in parse_cut_segments(asp_path) if bbox(s)["w"] >= MIN_DIM and bbox(s)["h"] >= MIN_DIM]
asp = [apply_offset(b, *OFFSET) for b in asp_raw]

print("=== COMPARACAO CHAPA 1 (contornos Z<=0, pecas >=200mm) ===\n")
print(f"Traços: {len(tr)} pecas | Aspire: {len(asp_raw)} pecas")
print(f"Offset Aspire->Traços: +{OFFSET[0]} mm X, +{OFFSET[1]} mm Y\n")

used_asp = set()
rows = []
for i, t in enumerate(tr, 1):
    candidates = [j for j, a in enumerate(asp) if size_close(t, a) and j not in used_asp]
    if not candidates:
        rows.append((i, t, None, "SEM PAR TAMANHO"))
        continue
    best_j = min(candidates, key=lambda j: abs(t["minx"] - asp[j]["minx"]) + abs(t["miny"] - asp[j]["miny"]))
    used_asp.add(best_j)
    a = asp[best_j]
    if pos_close(t, a):
        status = "BATE (pos+size)"
    elif size_close(t, a):
        status = f"SIZE OK, pos delta ({t['minx']-a['minx']:+.0f},{t['miny']-a['miny']:+.0f})"
    else:
        status = "DIVERGE"
    rows.append((i, t, (best_j + 1, a), status))

pos_ok = sum(1 for r in rows if r[3].startswith("BATE"))
size_ok = sum(1 for r in rows if r[2] is not None)

for i, t, match, status in rows:
    print(f"T{i}: ({t['minx']:.0f},{t['miny']:.0f}) {t['w']:.0f}x{t['h']:.0f}")
    if match:
        j, a = match
        print(f"  -> A{j}: ({a['minx']:.0f},{a['miny']:.0f}) {a['w']:.0f}x{a['h']:.0f}  [{status}]")
    else:
        print(f"  -> [{status}]")
    print()

print(f"Resumo: {pos_ok}/{len(tr)} batem posicao+tamanho | {size_ok}/{len(tr)} batem tamanho")
if len(asp_raw) > len(tr):
    print(f"Aspire tem {len(asp_raw)-len(tr)} contorno(s) extra(s)")
elif len(tr) > len(asp_raw):
    print(f"Traços tem {len(tr)-len(asp_raw)} peca(s) a mais no .tap")

print("\nLista tamanhos Traços:", sorted(norm_size(b) for b in tr))
print("Lista tamanhos Aspire:", sorted(norm_size(b) for b in asp_raw))
