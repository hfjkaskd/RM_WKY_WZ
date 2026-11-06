#!/usr/bin/env python3
"""Use or refresh the local Swagger cache for APIReplace.

This utility never reads ParamTemp.md.  It only manages LocalAPIData and can
list operations under an exact Swagger Tag.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import tempfile
import urllib.request
from collections import defaultdict
from datetime import datetime
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
DATA_DIR = ROOT / "LocalAPIData"
CACHE_PATH = DATA_DIR / "swagger-doc.json"
META_PATH = DATA_DIR / "swagger-doc.meta.json"
INDEX_PATH = DATA_DIR / "API接口目录.md"
DEFAULT_SOURCE = "http://test.api.pandamerge.top/swagger/doc.json"
METHODS = {"get", "post", "put", "patch", "delete", "head", "options"}


def atomic_write(path: Path, data: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(dir=path.parent, prefix=f".{path.name}.", delete=False) as handle:
        temporary = Path(handle.name)
        handle.write(data)
    os.replace(temporary, path)


def load_json(path: Path) -> dict[str, Any]:
    document = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(document, dict) or not (document.get("swagger") or document.get("openapi")):
        raise ValueError("不是有效的 Swagger/OpenAPI 文档")
    if not isinstance(document.get("paths"), dict):
        raise ValueError("Swagger/OpenAPI 文档没有 paths")
    return document


def fetch_document(source: str) -> bytes:
    request = urllib.request.Request(source, headers={"Accept": "application/json"})
    with urllib.request.urlopen(request, timeout=60) as response:
        data = response.read()
    load_json_bytes(data)
    return data


def load_json_bytes(data: bytes) -> dict[str, Any]:
    document = json.loads(data.decode("utf-8-sig"))
    if not isinstance(document, dict) or not (document.get("swagger") or document.get("openapi")):
        raise ValueError("下载内容不是有效的 Swagger/OpenAPI 文档")
    if not isinstance(document.get("paths"), dict):
        raise ValueError("下载的 Swagger/OpenAPI 文档没有 paths")
    return document


def operations(document: dict[str, Any]) -> list[dict[str, Any]]:
    result: list[dict[str, Any]] = []
    for path, path_item in document.get("paths", {}).items():
        if not isinstance(path_item, dict):
            continue
        for method, operation in path_item.items():
            if method.lower() not in METHODS or not isinstance(operation, dict):
                continue
            result.append(
                {
                    "path": str(path),
                    "method": method.upper(),
                    "tags": [str(tag) for tag in operation.get("tags") or []],
                    "operation_id": str(operation.get("operationId") or "-"),
                    "summary": str(operation.get("summary") or operation.get("description") or "-"),
                }
            )
    return sorted(result, key=lambda item: (item["path"], item["method"]))


def clean(value: str) -> str:
    return value.replace("|", r"\|").replace("\r", " ").replace("\n", " ").strip()


def write_metadata(document: dict[str, Any], source: str, fetched_at: str, raw: bytes) -> None:
    ops = operations(document)
    tags = {tag for operation in ops for tag in operation["tags"]} or {"(无Tag)"}
    metadata = {
        "source_url": source,
        "local_file": str(CACHE_PATH),
        "fetched_at": fetched_at,
        "swagger_version": document.get("swagger") or document.get("openapi"),
        "path_count": len(document.get("paths", {})),
        "operation_count": len(ops),
        "tag_count": len(tags),
        "definition_count": len(document.get("definitions") or document.get("components", {}).get("schemas", {}) or {}),
        "sha256": hashlib.sha256(raw).hexdigest(),
    }
    atomic_write(META_PATH, (json.dumps(metadata, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))


def write_index(document: dict[str, Any]) -> None:
    grouped: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for operation in operations(document):
        tags = operation["tags"] or ["(无Tag)"]
        for tag in tags:
            grouped[tag].append(operation)

    lines = [
        "# API 接口本地目录",
        "",
        "> 本文档由 `swagger-doc.json` 生成；字段详情以本地 Swagger 模型定义为准。",
        "",
        f"- 接口路径：{len(document.get('paths', {}))}",
        f"- 接口操作：{len(operations(document))}",
        f"- Tag：{len(grouped)}",
        "",
        "## Tag 索引",
        "",
        "| Tag | 接口数 |",
        "|---|---:|",
    ]
    for tag in sorted(grouped):
        anchor = tag.lower().replace(" ", "-")
        lines.append(f"| [{clean(tag)}](#{anchor}) | {len(grouped[tag])} |")
    lines.extend(["", "## 接口明细", ""])
    for tag in sorted(grouped):
        lines.extend(
            [
                f"### {tag}",
                "",
                "| 方法 | 路径 | OperationId | 摘要 |",
                "|---|---|---|---|",
            ]
        )
        for operation in grouped[tag]:
            lines.append(
                "| "
                + " | ".join(
                    [
                        clean(operation["method"]),
                        clean(operation["path"]),
                        clean(operation["operation_id"]),
                        clean(operation["summary"]),
                    ]
                )
                + " |"
            )
        lines.append("")
    atomic_write(INDEX_PATH, ("\n".join(lines) + "\n").encode("utf-8"))


def main() -> int:
    parser = argparse.ArgumentParser(description="使用或刷新 APIReplace 的本地 Swagger 缓存")
    parser.add_argument("--source", default=DEFAULT_SOURCE, help="Swagger JSON 地址")
    parser.add_argument("--refresh", action="store_true", help="忽略本地缓存并重新下载")
    parser.add_argument("--tag", help="按精确 Tag 列出接口")
    args = parser.parse_args()

    used_cache = False
    if CACHE_PATH.exists() and not args.refresh:
        try:
            document = load_json(CACHE_PATH)
            used_cache = True
            raw = CACHE_PATH.read_bytes()
        except Exception:
            document = None
            raw = b""
    else:
        document = None
        raw = b""

    if document is None:
        raw = fetch_document(args.source)
        document = load_json_bytes(raw)
        atomic_write(CACHE_PATH, raw)
        write_metadata(document, args.source, datetime.now().astimezone().isoformat(timespec="seconds"), raw)
        write_index(document)

    all_operations = operations(document)
    if not used_cache:
        write_index(document)
    selected = all_operations
    if args.tag:
        selected = [item for item in all_operations if args.tag in item["tags"]]

    print(f"来源：{'本地缓存' if used_cache else '重新下载'}")
    print(f"缓存：{CACHE_PATH}")
    print(f"接口操作：{len(all_operations)}")
    print(f"Tag：{len({tag for item in all_operations for tag in item['tags']})}")
    if args.tag:
        print(f"目标 Tag：{args.tag}")
        print(f"Tag 接口：{len(selected)}")
        for item in selected:
            print(f"{item['method']}\t{item['path']}\t{item['operation_id']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
