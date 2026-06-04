import argparse
import json
import os
import time
import urllib.parse
import urllib.request


def request_json(url, method="GET", body=None):
    data = None
    headers = {}
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        headers["Content-Type"] = "application/json"

    req = urllib.request.Request(url, data=data, headers=headers, method=method)
    with urllib.request.urlopen(req, timeout=30) as response:
        raw = response.read()
    return json.loads(raw.decode("utf-8"))


def download_file(url, target):
    os.makedirs(os.path.dirname(target), exist_ok=True)
    with urllib.request.urlopen(url, timeout=60) as response:
        data = response.read()
    with open(target, "wb") as handle:
        handle.write(data)


def build_prompt(asset, config):
    if config.get("backend") == "local_flux":
        return build_local_flux_prompt(asset, config)

    node = config.get("node", "StabilityStableImageUltraNode")
    positive = asset["prompt"]
    negative = asset.get("negative_prompt", config.get("negative_prompt", ""))
    if negative:
        positive = positive + "\nAvoid: " + negative

    return {
        "1": {
            "class_type": node,
            "inputs": {
                "prompt": positive,
                "aspect_ratio": asset.get("aspect_ratio", "1:1"),
                "style_preset": asset.get("style_preset", config.get("style_preset", "fantasy-art")),
                "seed": int(asset.get("seed", 0)),
            },
        },
        "2": {
            "class_type": "SaveImage",
            "inputs": {
                "images": ["1", 0],
                "filename_prefix": "DragonTD/" + asset["id"],
            },
        },
    }


def build_local_flux_prompt(asset, config):
    positive = sanitize_generation_prompt(asset["prompt"])
    negative = asset.get("negative_prompt", config.get("negative_prompt", ""))
    if negative:
        positive = positive + "\nAvoid: " + negative

    width = int(asset.get("width", config.get("width", 1024)))
    height = int(asset.get("height", config.get("height", 1024)))

    return {
        "1": {
            "class_type": "UNETLoader",
            "inputs": {
                "unet_name": asset.get("model", config.get("model", "flux1-schnell.safetensors")),
                "weight_dtype": asset.get("weight_dtype", config.get("weight_dtype", "default")),
            },
        },
        "2": {
            "class_type": "DualCLIPLoader",
            "inputs": {
                "clip_name1": config.get("clip_l", "clip_l.safetensors"),
                "clip_name2": config.get("clip_t5", "t5xxl_fp8_e4m3fn.safetensors"),
                "type": "flux",
            },
        },
        "3": {
            "class_type": "VAELoader",
            "inputs": {
                "vae_name": asset.get("vae", config.get("vae", "ae.safetensors")),
            },
        },
        "4": {
            "class_type": "CLIPTextEncode",
            "inputs": {
                "text": positive,
                "clip": ["2", 0],
            },
        },
        "5": {
            "class_type": "FluxGuidance",
            "inputs": {
                "conditioning": ["4", 0],
                "guidance": float(asset.get("guidance", config.get("guidance", 3.5))),
            },
        },
        "6": {
            "class_type": "CLIPTextEncode",
            "inputs": {
                "text": "",
                "clip": ["2", 0],
            },
        },
        "7": {
            "class_type": "EmptyLatentImage",
            "inputs": {
                "width": width,
                "height": height,
                "batch_size": 1,
            },
        },
        "8": {
            "class_type": "KSampler",
            "inputs": {
                "model": ["1", 0],
                "seed": int(asset.get("seed", 0)),
                "steps": int(asset.get("steps", config.get("steps", 4))),
                "cfg": float(asset.get("cfg", config.get("cfg", 1.0))),
                "sampler_name": asset.get("sampler", config.get("sampler", "euler")),
                "scheduler": asset.get("scheduler", config.get("scheduler", "simple")),
                "positive": ["5", 0],
                "negative": ["6", 0],
                "latent_image": ["7", 0],
                "denoise": 1.0,
            },
        },
        "9": {
            "class_type": "VAEDecode",
            "inputs": {
                "samples": ["8", 0],
                "vae": ["3", 0],
            },
        },
        "10": {
            "class_type": "SaveImage",
            "inputs": {
                "images": ["9", 0],
                "filename_prefix": "DragonTD/" + asset["id"],
            },
        },
    }


def sanitize_generation_prompt(prompt):
    return (
        prompt.replace("Dragon Dominion tower defense RPG asset, ", "fantasy tower defense RPG asset, ")
        .replace("Dragon Dominion tower defense enemy sprite, ", "fantasy tower defense enemy sprite, ")
        .replace("Dragon Dominion", "fantasy tower defense RPG")
    )


def wait_for_output(base_url, prompt_id, timeout_seconds):
    deadline = time.time() + timeout_seconds
    history_url = base_url.rstrip("/") + "/history/" + prompt_id

    while time.time() < deadline:
        history = request_json(history_url)
        entry = history.get(prompt_id)
        if not entry:
            time.sleep(2)
            continue

        status = entry.get("status", {})
        if status.get("status_str") == "error":
            messages = status.get("messages", [])
            raise RuntimeError(json.dumps(messages, indent=2))

        outputs = entry.get("outputs") or {}
        for output in outputs.values():
            images = output.get("images") or []
            if images:
                return images[0]

        time.sleep(2)

    raise TimeoutError("Timed out waiting for ComfyUI output: " + prompt_id)


def run_asset(base_url, asset, config, repo_root, timeout_seconds):
    prompt = build_prompt(asset, config)
    result = request_json(base_url.rstrip() + "/prompt", method="POST", body={"prompt": prompt})
    prompt_id = result["prompt_id"]
    print("queued", asset["id"], prompt_id)

    image = wait_for_output(base_url, prompt_id, timeout_seconds)
    query = urllib.parse.urlencode(
        {
            "filename": image["filename"],
            "subfolder": image.get("subfolder", ""),
            "type": image.get("type", "output"),
        }
    )
    source_url = base_url.rstrip("/") + "/view?" + query
    target = os.path.join(repo_root, asset["target"].replace("/", os.sep))
    download_file(source_url, target)
    print("saved", target)


def main():
    parser = argparse.ArgumentParser(description="Generate DragonTD assets through local ComfyUI.")
    parser.add_argument("--manifest", default="tools/comfy_assets_phase1.json")
    parser.add_argument("--only", nargs="*", help="Optional asset ids to generate.")
    parser.add_argument("--timeout", type=int, default=180)
    args = parser.parse_args()

    repo_root = os.getcwd()
    with open(args.manifest, "r", encoding="utf-8") as handle:
        config = json.load(handle)

    selected = set(args.only or [])
    assets = [asset for asset in config["assets"] if not selected or asset["id"] in selected]
    if not assets:
        raise SystemExit("No matching assets found.")

    base_url = config.get("comfy_url", "http://127.0.0.1:8000")
    for asset in assets:
        run_asset(base_url, asset, config, repo_root, args.timeout)


if __name__ == "__main__":
    main()
