"""
Processes ComfyUI-generated dragon images into deployed tower sprites:
- Crops to the largest dragon in the image
- Removes white/near-white background
- Saves to Unity/Assets/Art/Dragons/{dragonId}/{dragonId}_deployed.png  (256x256 RGBA PNG)
"""
from PIL import Image
import numpy as np
import os

COMFY_OUTPUT  = r"C:\Users\mikeo\Documents\ComfyUI\output"
UNITY_DRAGONS = r"D:\DragonTD\Unity\Assets\Art\Dragons"

DRAGON_MAP = [
    ("flux_schnell_00003_.png", "celestara_006"),
    ("flux_schnell_00004_.png", "shadowfang_007"),
    ("flux_schnell_00005_.png", "stonehide_005"),
    ("flux_schnell_00006_.png", "tempest_glacion_004"),
    ("flux_schnell_00007_.png", "emberveil_008"),
    ("flux_schnell_00008_.png", "tideclaw_009"),
    ("flux_schnell_00009_.png", "zephyrwing_010"),
]

def remove_white_bg(img, threshold=238):
    img = img.convert("RGBA")
    data = np.array(img)
    r, g, b, a = data[...,0], data[...,1], data[...,2], data[...,3]
    bg = (r > threshold) & (g > threshold) & (b > threshold)
    data[...,3] = np.where(bg, 0, a)
    return Image.fromarray(data)

def crop_largest_blob(img_rgba, padding=20):
    alpha = np.array(img_rgba.getchannel('A'))
    row_proj = alpha.sum(axis=1)
    col_proj = alpha.sum(axis=0)
    active_rows = np.where(row_proj > 0)[0]
    active_cols = np.where(col_proj > 0)[0]
    if len(active_rows) == 0:
        return img_rgba

    # Find blobs separated by large gaps (> 25px) — pick the tallest
    row_gaps  = np.where(np.diff(active_rows) > 25)[0]
    row_splits = [0] + list(row_gaps + 1) + [len(active_rows)]
    best_h, best_box = 0, None
    for i in range(len(row_splits) - 1):
        r0 = int(active_rows[row_splits[i]])
        r1 = int(active_rows[row_splits[i+1] - 1])
        region = alpha[r0:r1+1, :]
        cols = np.where(region.sum(axis=0) > 0)[0]
        if len(cols) == 0:
            continue
        h = r1 - r0
        if h > best_h:
            best_h = h
            best_box = (int(cols[0]), r0, int(cols[-1])+1, r1+1)

    if best_box is None:
        best_box = (int(active_cols[0]), int(active_rows[0]),
                    int(active_cols[-1])+1, int(active_rows[-1])+1)

    W, H = img_rgba.size
    l = max(0, best_box[0] - padding)
    t = max(0, best_box[1] - padding)
    r = min(W, best_box[2] + padding)
    b = min(H, best_box[3] + padding)
    cropped = img_rgba.crop((l, t, r, b))

    # Pad to square so Unity's PPU keeps it centred
    cw, ch = cropped.size
    side = max(cw, ch)
    square = Image.new("RGBA", (side, side), (0,0,0,0))
    square.paste(cropped, ((side-cw)//2, (side-ch)//2))
    return square

def process(src, dst_dir, dragon_id):
    img    = Image.open(src)
    no_bg  = remove_white_bg(img)
    cropped = crop_largest_blob(no_bg)
    result  = cropped.resize((256, 256), Image.LANCZOS)
    os.makedirs(dst_dir, exist_ok=True)
    out = os.path.join(dst_dir, f"{dragon_id}_deployed.png")
    result.save(out, "PNG")
    print(f"  ✓  {dragon_id} -> {out}")

print("Processing deployed dragon sprites...")
for filename, dragon_id in DRAGON_MAP:
    src = os.path.join(COMFY_OUTPUT, filename)
    if not os.path.exists(src):
        print(f"  ✗  MISSING: {filename}"); continue
    try:
        process(src, os.path.join(UNITY_DRAGONS, dragon_id), dragon_id)
    except Exception as e:
        print(f"  ✗  {dragon_id}: {e}")
print("Done.")
