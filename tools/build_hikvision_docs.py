#!/usr/bin/env python3
"""
下载海康官方 SDK/产品页面文档并生成适合 AI 检索的 Markdown 索引。

输入：
- 仓库中的 HCNetSDK 绑定文件（HCNetSDK.cs / HCNetSDK_Facial.cs）
- 海康官网和开放平台可访问的 HTML/PDF 文档

输出：
- 指定输出目录下的 Markdown 文档、目录索引、AI 索引、项目接口对照
- cache/ 下的原始 HTML/PDF 缓存
"""

from __future__ import annotations

import argparse
import hashlib
import html
import json
import os
import re
import shutil
import subprocess
import sys
import textwrap
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable
from urllib import error, parse, request


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT = ROOT / "设备网络SDK编程指南（明眸-以人为中心）"
USER_AGENT = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
    "(KHTML, like Gecko) Chrome/135.0 Safari/537.36 CodexDocBuilder/1.0"
)

SDK_BINDINGS = [
    ROOT / "HCNetSDK.cs",
    ROOT / "HCNetSDK_Facial.cs",
]

PROJECT_HOTSPOTS = [
    "NET_DVR_Login_V40",
    "NET_DVR_STDXMLConfig",
    "NET_DVR_GetDeviceAbility",
    "NET_DVR_RealPlay_V40",
    "NET_DVR_CaptureJPEGPicture",
    "NET_DVR_StartRemoteConfig",
    "NET_DVR_SendWithRecvRemoteConfig",
    "NET_DVR_ACS_WORK_STATUS_V50",
    "NET_DVR_USER_LOGIN_INFO",
]


@dataclass(frozen=True)
class PdfSource:
    slug: str
    category: str
    title: str
    source_url: str
    target_pdf_name: str
    keywords: list[str]


@dataclass(frozen=True)
class HtmlSource:
    slug: str
    category: str
    source_url: str
    kind: str
    keywords: list[str]


@dataclass
class ArtifactRecord:
    doc_type: str
    category: str
    title: str
    source_url: str
    local_path: str
    symbol_or_topic: str
    keywords: list[str] = field(default_factory=list)
    summary: str = ""
    project_usage: list[dict[str, object]] = field(default_factory=list)
    section_anchor: str | None = None
    raw_cache_path: str | None = None
    related_symbols: list[str] = field(default_factory=list)
    fetched_at: str | None = None
    sha256: str | None = None
    notes: list[str] = field(default_factory=list)


PDF_SOURCES = [
    PdfSource(
        slug="superbrain-ids-manual",
        category="superbrain",
        title="iDS 智脑网络硬盘录像机（79 S 系列）操作手册",
        source_url="https://www.hikvision.com/content/dam/hikvision/products/S000000365/S000000641/S000001011/S000000652/OFR001036/M000032007/SM000022028/%E6%93%8D%E4%BD%9C%E6%89%8B%E5%86%8C/UD18776B_%E6%B5%B7%E5%BA%B7%E5%A8%81%E8%A7%86iDS%E6%99%BA%E8%84%91%E7%BD%91%E7%BB%9C%E7%A1%AC%E7%9B%98%E5%BD%95%E5%83%8F%E6%9C%BA%EF%BC%8879-S%E7%B3%BB%E5%88%97%EF%BC%89_%E6%93%8D%E4%BD%9C%E6%89%8B%E5%86%8C_V4.22.400_20200327.pdf",
        target_pdf_name="UD18776B_海康威视iDS智脑网络硬盘录像机（79-S系列）_操作手册_V4.22.400_20200327.pdf",
        keywords=["超脑", "iDS", "智脑", "NVR", "操作手册"],
    ),
    PdfSource(
        slug="hcwebsdk-guide",
        category="network-camera",
        title="HCWebSDK V3.3.0 编程指南",
        source_url="https://open.hikvision.com/fileserver/resourcedocsonline/HCWebSDK3.3.0%E7%BC%96%E7%A8%8B%E6%8C%87%E5%8D%97_20230420134609.pdf",
        target_pdf_name="HCWebSDK3.3.0编程指南_20230420134609.pdf",
        keywords=["HCWebSDK", "视频预览", "Web", "摄像机", "浏览器"],
    ),
    PdfSource(
        slug="web3-control-guide",
        category="network-camera",
        title="Web 3.0 控件开发包编程指南",
        source_url="https://open.hikvision.com/fileserver/resourcedocsonline/Web3.0_%E6%8E%A7%E4%BB%B6%E5%BC%80%E5%8F%91%E5%8C%85%E7%BC%96%E7%A8%8B%E6%8C%87%E5%8D%97_20201102162751.pdf",
        target_pdf_name="Web3.0_控件开发包编程指南_20201102162751.pdf",
        keywords=["Web3.0", "控件", "视频预览", "网络摄像头", "浏览器"],
    ),
    PdfSource(
        slug="network-camera-g5-manual",
        category="network-camera",
        title="海康威视网络摄像机操作手册 G5",
        source_url="https://www.hikvision.com/content/dam/hikvision/products/S000000365/S000000509/S000000534/S000000983/OFR011448/M000063304/SM000044226/%E7%94%A8%E6%88%B7%E6%89%8B%E5%86%8C/UD29694B_%E6%B5%B7%E5%BA%B7%E5%A8%81%E8%A7%86%E7%BD%91%E7%BB%9C%E6%91%84%E5%83%8F%E6%9C%BA%E6%93%8D%E4%BD%9C%E6%89%8B%E5%86%8CG5_V5.7.50_20220809.PDF",
        target_pdf_name="UD29694B_海康威视网络摄像机操作手册G5_V5.7.50_20220809.PDF",
        keywords=["网络摄像机", "用户手册", "操作手册", "G5", "摄像头"],
    ),
    PdfSource(
        slug="video-web-plugin-guide",
        category="network-camera",
        title="视频WEB插件 V1.5.2 开发指南",
        source_url="https://open.hikvision.com/fileserver/resourcedocsonline/%E8%A7%86%E9%A2%91WEB%E6%8F%92%E4%BB%B6V1.5.2%E5%BC%80%E5%8F%91%E6%8C%87%E5%8D%97_20210916162843_20210918161557.pdf",
        target_pdf_name="视频WEB插件V1.5.2开发指南_20210918161557.pdf",
        keywords=["Web插件", "视频预览", "网络摄像头", "浏览器", "控件"],
    ),
]


HTML_SOURCES = [
    HtmlSource(
        slug="superbrain-product-page",
        category="superbrain",
        source_url="https://www.hikvision.com/cn/products/pdplist/65730/",
        kind="product_page",
        keywords=["超脑", "iDS", "NVR", "下载中心", "用户手册"],
    ),
    HtmlSource(
        slug="superbrain-user-manual-detail",
        category="superbrain",
        source_url="https://partners.hikvision.com/material-product/detail/579534258720653312?type=pdf&source=gw",
        kind="partner_detail",
        keywords=["超脑", "用户手册", "iDS", "NVR"],
    ),
    HtmlSource(
        slug="superbrain-web-manual-detail",
        category="superbrain",
        source_url="https://partners.hikvision.com/material-product/detail/579534259735674880?type=pdf&source=gw",
        kind="partner_detail",
        keywords=["超脑", "NVR Web", "用户手册", "Web"],
    ),
    HtmlSource(
        slug="network-camera-product-page",
        category="network-camera",
        source_url="https://www.hikvision.com/cn/products/pdplist/100381/",
        kind="product_page",
        keywords=["网络摄像机", "WiFi", "下载中心", "用户手册"],
    ),
    HtmlSource(
        slug="network-camera-user-manual-detail",
        category="network-camera",
        source_url="https://partners.hikvision.com/material-product/detail/640614855664640000?type=pdf&source=gw",
        kind="partner_detail",
        keywords=["网络摄像机", "用户手册", "H8"],
    ),
]


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def rel_path(path: Path) -> str:
    return path.resolve().relative_to(ROOT).as_posix()


def ensure_dir(path: Path) -> Path:
    path.mkdir(parents=True, exist_ok=True)
    return path


def slugify(value: str) -> str:
    lowered = value.lower()
    lowered = re.sub(r"[^0-9a-zA-Z\u4e00-\u9fff]+", "-", lowered)
    lowered = re.sub(r"-{2,}", "-", lowered).strip("-")
    return lowered or "doc"


def clean_text(value: str) -> str:
    value = html.unescape(value)
    value = re.sub(r"<br\s*/?>", "\n", value, flags=re.IGNORECASE)
    value = re.sub(r"</?(?:b|strong|i|em|span|font|u|sup|sub)[^>]*>", "", value, flags=re.IGNORECASE)
    value = re.sub(r"</p\s*>", "\n\n", value, flags=re.IGNORECASE)
    value = re.sub(r"</?(?:p|div|li|ul|ol|table|tbody|thead|tr|td|th|dl|dd|dt)[^>]*>", "\n", value, flags=re.IGNORECASE)
    value = re.sub(r"<[^>]+>", "", value)
    value = value.replace("\r", "")
    value = re.sub(r"\n{3,}", "\n\n", value)
    return value.strip()


def markdown_escape_link_text(value: str) -> str:
    return value.replace("[", "\\[").replace("]", "\\]")


def http_fetch(url: str, *, refresh: bool, offline: bool, cache_path: Path) -> tuple[bytes, str]:
    ensure_dir(cache_path.parent)
    if cache_path.exists() and not refresh:
        return cache_path.read_bytes(), "cache"
    if offline:
        raise FileNotFoundError(f"离线模式下缺少缓存：{cache_path}")

    req = request.Request(url, headers={"User-Agent": USER_AGENT})
    with request.urlopen(req, timeout=60) as resp:
        data = resp.read()
    cache_path.write_bytes(data)
    return data, "network"


def decode_html_blob(data: bytes) -> str:
    head = data[:2048].decode("latin1", errors="ignore")
    meta = re.search(r"charset=([a-zA-Z0-9_-]+)", head, flags=re.IGNORECASE)
    encodings = []
    if meta:
        encodings.append(meta.group(1))
    encodings.extend(["utf-8", "gb18030", "gbk", "gb2312", "latin1"])
    seen: set[str] = set()
    for encoding in encodings:
        lowered = encoding.lower()
        if lowered in seen:
            continue
        seen.add(lowered)
        try:
            return data.decode(encoding)
        except UnicodeDecodeError:
            continue
    return data.decode("utf-8", errors="replace")


def extract_title(text: str) -> str:
    for pattern in [
        r"<h1[^>]*>(.*?)</h1>",
        r"<title>(.*?)</title>",
    ]:
        match = re.search(pattern, text, flags=re.IGNORECASE | re.DOTALL)
        if match:
            return clean_text(match.group(1))
    return "未命名文档"


def extract_meta_description(text: str) -> str:
    match = re.search(
        r'<meta[^>]+name=["\']description["\'][^>]+content=["\'](.*?)["\']',
        text,
        flags=re.IGNORECASE | re.DOTALL,
    )
    return clean_text(match.group(1)) if match else ""


def extract_first_paragraph(text: str) -> str:
    match = re.search(r"<p[^>]*>(.*?)</p>", text, flags=re.IGNORECASE | re.DOTALL)
    return clean_text(match.group(1)) if match else ""


def html_to_markdown_sdk(symbol: str, category: str, source_url: str, text: str) -> tuple[str, str, str]:
    title = extract_title(text)
    summary = extract_first_paragraph(text)
    syntax_match = re.search(
        r"<pre[^>]*class=[\"']syntax[\"'][^>]*>(.*?)</pre>",
        text,
        flags=re.IGNORECASE | re.DOTALL,
    )
    syntax_block = clean_text(syntax_match.group(1)) if syntax_match else ""

    body_parts = [f"# {title}", "", f"- 来源：[{source_url}]({source_url})", ""]
    if summary:
        body_parts.extend([summary, ""])
    if syntax_block:
        body_parts.extend(["## 语法", "", "```c", syntax_block, "```", ""])

    section_pattern = re.compile(
        r"<h4[^>]*>(.*?)</h4>(.*?)(?=<h4[^>]*>|</body>|$)",
        flags=re.IGNORECASE | re.DOTALL,
    )
    found_section = False
    for match in section_pattern.finditer(text):
        found_section = True
        section_title = clean_text(match.group(1)) or "未命名节"
        section_html = match.group(2)
        body_parts.extend([f"## {section_title}", ""])

        dl_matches = re.findall(
            r"<dt[^>]*>(.*?)</dt>\s*<dd[^>]*>(.*?)</dd>",
            section_html,
            flags=re.IGNORECASE | re.DOTALL,
        )
        if dl_matches:
            for dt_html, dd_html in dl_matches:
                body_parts.append(f"- `{clean_text(dt_html)}`：{clean_text(dd_html)}")
            body_parts.append("")
            continue

        pre_matches = re.findall(r"<pre[^>]*>(.*?)</pre>", section_html, flags=re.IGNORECASE | re.DOTALL)
        if pre_matches:
            for pre_html in pre_matches:
                body_parts.extend(["```text", clean_text(pre_html), "```", ""])

        paragraphs = re.findall(r"<p[^>]*>(.*?)</p>", section_html, flags=re.IGNORECASE | re.DOTALL)
        if paragraphs:
            for paragraph in paragraphs:
                cleaned = clean_text(paragraph)
                if cleaned:
                    body_parts.extend([cleaned, ""])
        else:
            cleaned = clean_text(section_html)
            if cleaned:
                body_parts.extend([cleaned, ""])

    if not found_section:
        cleaned = clean_text(text)
        if cleaned:
            body_parts.extend(["## 原文摘录", "", cleaned[:4000], ""])

    related_links = []
    for href, label in re.findall(r'<a\s+href="([^"]+)"[^>]*>(.*?)</a>', text, flags=re.IGNORECASE | re.DOTALL):
        cleaned_label = clean_text(label)
        if not cleaned_label:
            continue
        if href.startswith("../definitions/"):
            local = "../definitions/" + Path(href).stem + ".md"
        elif href.startswith("../structures/"):
            local = "../structures/" + Path(href).stem + ".md"
        elif href.endswith(".html") and not href.startswith("http"):
            local = href[:-5] + ".md"
        else:
            local = parse.urljoin(source_url, href)
        related_links.append(f"- [{markdown_escape_link_text(cleaned_label)}]({local})")
    if related_links:
        deduped = []
        seen = set()
        for item in related_links:
            if item in seen:
                continue
            seen.add(item)
            deduped.append(item)
        body_parts.extend(["## 相关链接", "", *deduped, ""])

    markdown = "\n".join(part.rstrip() for part in body_parts).strip() + "\n"
    summary_text = summary or f"{symbol} 的海康设备网络 SDK 官方页面。"
    return title, markdown if markdown else "", summary_text


def parse_download_cards(text: str) -> list[tuple[str, str]]:
    cards = []
    seen = set()
    pattern = re.compile(
        r'<a[^>]+class="download-card[^"]*"[^>]+href="([^"]+)"[^>]*>.*?<div[^>]+class="card-desc[^"]*"[^>]*>(.*?)</div>',
        flags=re.IGNORECASE | re.DOTALL,
    )
    for href, title_html in pattern.findall(text):
        title = clean_text(title_html)
        if title:
            normalized = (html.unescape(href), title)
            if normalized in seen:
                continue
            seen.add(normalized)
            cards.append(normalized)
    return cards


def html_to_markdown_product_page(source: HtmlSource, text: str) -> tuple[str, str, list[str]]:
    title = extract_title(text)
    desc = extract_meta_description(text)
    breadcrumb_titles = [
        clean_text(item)
        for item in re.findall(r'<div class="nav-item[^"]*">.*?(?:<a[^>]*class="link[^"]*"[^>]*>(.*?)</a>|<h1[^>]*>(.*?)</h1>)', text, flags=re.IGNORECASE | re.DOTALL)
        for item in item if item
    ]
    downloads = parse_download_cards(text)

    lines = [f"# {title}", "", f"- 来源：[{source.source_url}]({source.source_url})", ""]
    if desc:
        lines.extend([desc, ""])
    if breadcrumb_titles:
        lines.extend(["## 页面路径", "", " > ".join(breadcrumb_titles), ""])
    lines.extend([
        "## 下载中心",
        "",
        "以下条目来自海康官网产品页的“下载中心”，链接通常会跳转到海康合作伙伴资料页。",
        "",
    ])
    for href, card_title in downloads:
        lines.append(f"- [{markdown_escape_link_text(card_title)}]({html.unescape(href)})")
    if not downloads:
        lines.append("- 未在页面中解析到下载中心条目。")
    lines.append("")
    return title, "\n".join(lines).strip() + "\n", [title for _, title in downloads]


def extract_partner_detail_metadata(text: str) -> dict[str, str]:
    metadata = {}
    patterns = {
        "classification": r'classification:"([^"]+)"',
        "title": r'title:"([^"]+)"',
        "describe": r'describe:"([^"]+)"',
        "fileName": r'fileName:"([^"]+)"',
        "fileFormat": r'fileFormat:"([^"]+)"',
        "modifiedTime": r'modifiedTime:"([^"]+)"',
        "size": r'size:"([^"]+)"',
        "bookShelfOpenTypeStr": r'bookShelfOpenTypeStr:"([^"]+)"',
        "newKeyword": r'newKeyword:"([^"]+)"',
        "newDescribe": r'newDescribe:"([^"]+)"',
    }
    for key, pattern in patterns.items():
        match = re.search(pattern, text, flags=re.DOTALL)
        if match:
            metadata[key] = clean_text(match.group(1))
    if "title" not in metadata:
        metadata["title"] = extract_title(text)
    return metadata


def html_to_markdown_partner_detail(source: HtmlSource, text: str) -> tuple[str, str]:
    metadata = extract_partner_detail_metadata(text)
    title = metadata.get("title", extract_title(text))
    lines = [
        f"# {title}",
        "",
        f"- 来源：[{source.source_url}]({source.source_url})",
        f"- 分类：{metadata.get('classification', '未知')}",
        f"- 文件格式：{metadata.get('fileFormat', '未知')}",
        f"- 文件大小：{metadata.get('size', '未知')}",
        f"- 可见性：{metadata.get('bookShelfOpenTypeStr', '未知')}",
        f"- 更新时间：{metadata.get('modifiedTime', '未知')}",
        "",
    ]
    desc = metadata.get("describe") or metadata.get("newDescribe")
    if desc:
        lines.extend([desc, ""])
    keywords = metadata.get("newKeyword")
    if keywords:
        lines.extend(["## 关键词", "", keywords, ""])
    lines.extend([
        "## 说明",
        "",
        "该页面来自海康合作伙伴资料中心，当前脚本会保留详情页快照、资料标题、文件格式、大小和可见性信息。",
        "如果资料本体要求合作伙伴登录才能下载，脚本会在本地保留详情页快照，并在目录索引中显式标记。",
        "",
    ])
    return title, "\n".join(lines).strip() + "\n"


def split_markdown_by_headings(markdown_path: Path, sections_root: Path, base_slug: str) -> list[Path]:
    text = markdown_path.read_text(encoding="utf-8")
    lines = text.splitlines()
    sections = []
    current_heading = None
    current_lines: list[str] = []
    for line in lines:
        if re.match(r"^##\s+", line):
            if current_heading and current_lines:
                sections.append((current_heading, "\n".join(current_lines).strip() + "\n"))
            current_heading = re.sub(r"^##\s+", "", line).strip()
            current_lines = [f"# {current_heading}", ""]
        elif current_heading:
            current_lines.append(line)
    if current_heading and current_lines:
        sections.append((current_heading, "\n".join(current_lines).strip() + "\n"))

    created = []
    if len(sections) <= 1:
        return created
    out_dir = ensure_dir(sections_root / base_slug)
    for index, (heading, content) in enumerate(sections, start=1):
        path = out_dir / f"{index:02d}-{slugify(heading)}.md"
        path.write_text(content, encoding="utf-8")
        created.append(path)
    return created


def discover_symbols() -> tuple[list[str], list[str]]:
    functions: set[str] = set()
    structures: set[str] = set()
    for path in SDK_BINDINGS:
        if not path.exists():
            continue
        text = path.read_text(encoding="utf-8", errors="ignore")
        functions.update(
            re.findall(
                r"\bextern\s+[A-Za-z0-9_<>\[\]]+\s+(NET_DVR_[A-Za-z0-9_]+)\s*\(",
                text,
            )
        )
        structures.update(re.findall(r"\bstruct\s+(NET_DVR_[A-Za-z0-9_]+)\b", text))
    return sorted(functions), sorted(structures)


def find_usages(symbol: str, limit: int = 5) -> list[dict[str, object]]:
    usages: list[dict[str, object]] = []
    pattern = re.compile(rf"\b{re.escape(symbol)}\b")
    for path in ROOT.rglob("*.cs"):
        if any(part in {"bin", "obj"} for part in path.parts):
            continue
        try:
            lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
        except OSError:
            continue
        for line_number, line in enumerate(lines, start=1):
            if pattern.search(line):
                usages.append(
                    {
                        "path": rel_path(path),
                        "line": line_number,
                        "snippet": line.strip()[:180],
                    }
                )
                if len(usages) >= limit:
                    return usages
    return usages


def write_markdown(path: Path, content: str) -> None:
    ensure_dir(path.parent)
    path.write_text(content, encoding="utf-8")


def run_command(cmd: list[str], env: dict[str, str] | None = None) -> None:
    subprocess.run(cmd, check=True, env=env)


def setup_mineru(venv_dir: Path) -> Path:
    python_exe = venv_dir / "Scripts" / "python.exe"
    mineru_exe = venv_dir / "Scripts" / "mineru.exe"
    if not python_exe.exists():
        run_command(["py", "-3.12", "-m", "venv", str(venv_dir)])
    run_command([str(python_exe), "-m", "pip", "install", "-U", "pip"])
    run_command([str(python_exe), "-m", "pip", "install", '-U', "mineru[all]"])
    if not mineru_exe.exists():
        raise FileNotFoundError(f"未找到 MinerU CLI：{mineru_exe}")
    return mineru_exe


def detect_mineru_exe(venv_dir: Path) -> Path | None:
    mineru_exe = venv_dir / "Scripts" / "mineru.exe"
    return mineru_exe if mineru_exe.exists() else None


def convert_pdf_with_mineru(pdf_path: Path, mineru_exe: Path, work_dir: Path) -> tuple[Path | None, list[Path]]:
    ensure_dir(work_dir)
    env = os.environ.copy()
    env["MINERU_MODEL_SOURCE"] = "modelscope"
    run_command(
        [
            str(mineru_exe),
            "-p",
            str(pdf_path),
            "-o",
            str(work_dir),
            "-b",
            "pipeline",
        ],
        env=env,
    )
    md_files = sorted(work_dir.rglob("*.md"), key=lambda item: len(item.parts))
    primary = md_files[0] if md_files else None
    return primary, md_files


def copy_tree_if_exists(source: Path, target: Path) -> None:
    if not source.exists():
        return
    if source.is_file():
        ensure_dir(target.parent)
        shutil.copy2(source, target)
        return
    if target.exists():
        shutil.rmtree(target)
    shutil.copytree(source, target)


def render_root_readme(output_dir: Path, artifacts: list[ArtifactRecord]) -> str:
    counts = {}
    for artifact in artifacts:
        counts[artifact.category] = counts.get(artifact.category, 0) + 1
    lines = [
        "# 海康官方开发文档恢复包",
        "",
        "本目录用于补齐当前项目缺失的海康官方开发文档，并为 AI 检索提供拆分后的 Markdown、小文档和索引。",
        "",
        "## 内容概览",
        "",
        f"- `sdk/`：设备网络 SDK / ISAPI 文档，共 {counts.get('sdk', 0)} 项。",
        f"- `superbrain/`：超脑 / iDS / NVR 页面与资料详情，共 {counts.get('superbrain', 0)} 项。",
        f"- `network-camera/`：网络摄像头、Web 预览与视频侧文档，共 {counts.get('network-camera', 0)} 项。",
        "- `cache/`：原始 HTML / PDF 缓存，不建议纳入版本控制。",
        "",
        "## 使用方式",
        "",
        "- 从 [目录索引.md](目录索引.md) 进入分类目录。",
        "- 从 [项目接口对照.md](项目接口对照.md) 按当前项目里实际调用的接口跳转。",
        "- 从 `ai-index.json` 做程序化检索。",
        "",
        "## 构建命令",
        "",
        "```powershell",
        'python tools/build_hikvision_docs.py --output "设备网络SDK编程指南（明眸-以人为中心）" --scope full',
        "```",
        "",
        "如需自动安装 MinerU，可额外带上 `--setup-mineru`。",
        "",
    ]
    return "\n".join(lines).strip() + "\n"


def render_technical_spec(output_dir: Path, artifact_lookup: dict[str, ArtifactRecord]) -> str:
    def link_for(key: str, fallback: str) -> str:
        artifact = artifact_lookup.get(key)
        if not artifact:
            return fallback
        return f"[{artifact.title}]({Path(artifact.local_path).relative_to(output_dir).as_posix()})"

    lines = [
        "# 技术规范",
        "",
        "本页用于补齐仓库中约定的本地工作规范，并把当前项目最常用的海康资料入口整理为可检索索引。",
        "",
        "## 明眸 / 门禁 SDK 调用规范",
        "",
        f"- 初始化与登录优先查阅 {link_for('NET_DVR_Login_V40', 'NET_DVR_Login_V40')} 与 `NET_DVR_USER_LOGIN_INFO`。",
        f"- ISAPI/XML 透传优先查阅 {link_for('NET_DVR_STDXMLConfig', 'NET_DVR_STDXMLConfig')}。",
        f"- 状态检测与能力判断优先查阅 {link_for('NET_DVR_GetDeviceAbility', 'NET_DVR_GetDeviceAbility')} 与 `NET_DVR_ACS_WORK_STATUS_V50`。",
        "- 设备调用需遵循当前项目已有的设备级 SDK 锁设计，避免在同一设备上并发执行登录、远程配置、ISAPI 与布防操作。",
        "- 失败时统一读取 `NET_DVR_GetLastError`，并结合当前项目中的重试策略判断是否允许重试。",
        "",
        "## 超脑 / NVR 接入与预览说明",
        "",
        "- `superbrain/` 目录会保留超脑产品页、合作伙伴资料页快照，并尝试抓取可公开访问的 iDS/超脑侧官方 PDF。",
        "- 对于要求合作伙伴登录或 `content/dam` 直链受限的超脑资料，当前构建链路会保留来源地址、失败原因和索引，不会伪造或替换官方原文。",
        "- 若后续补充具体型号，可继续沿用当前目录结构与 MinerU 流程追加专属手册。",
        "",
        "## 网络摄像机预览、抓图与 Web 组件说明",
        "",
        f"- 实时预览查阅 {link_for('NET_DVR_RealPlay_V40', 'NET_DVR_RealPlay_V40')}。",
        f"- JPEG 抓图查阅 {link_for('NET_DVR_CaptureJPEGPicture', 'NET_DVR_CaptureJPEGPicture')}。",
        "- `network-camera/` 下会优先保留网络摄像机通用操作手册、HCWebSDK 与视频 WEB 插件等官方 PDF；若部分直链受限，会落地失败说明页，便于后续补抓。",
        "",
        "## 错误码、重试与并发约束",
        "",
        "- 错误码处理优先查阅 `NET_DVR_GetLastError` 以及当前项目的 `DeviceOperationRetryBehavior.cs`。",
        "- 涉及同设备的长耗时操作应先确认 `DeviceConnectionManager.cs` 中的设备锁和状态检查锁语义。",
        "- 布防、远程配置、抓图、预览等高频操作应控制调用频率，避免在设备忙或网络抖动时叠加放大问题。",
        "",
    ]
    return "\n".join(lines).strip() + "\n"


def render_directory_index(output_dir: Path, artifacts: list[ArtifactRecord]) -> str:
    groups: dict[str, list[ArtifactRecord]] = {}
    for artifact in sorted(artifacts, key=lambda item: (item.category, item.title.lower())):
        groups.setdefault(artifact.category, []).append(artifact)

    category_titles = {
        "sdk": "设备网络 SDK / ISAPI",
        "superbrain": "超脑 / iDS / NVR",
        "network-camera": "网络摄像头 / 视频侧",
    }

    lines = ["# 目录索引", ""]
    for category in ("sdk", "superbrain", "network-camera"):
        items = groups.get(category, [])
        lines.extend([f"## {category_titles.get(category, category)}", ""])
        if not items:
            lines.extend(["- 暂无文档。", ""])
            continue
        for artifact in items:
            local = Path(artifact.local_path).relative_to(output_dir).as_posix()
            usage_suffix = ""
            if artifact.project_usage:
                usage_suffix = f"；项目命中 {len(artifact.project_usage)} 处"
            lines.append(
                f"- [{markdown_escape_link_text(artifact.title)}]({local})：{artifact.summary or artifact.doc_type}{usage_suffix}"
            )
        lines.append("")
    return "\n".join(lines).strip() + "\n"


def render_project_mapping(output_dir: Path, artifact_lookup: dict[str, ArtifactRecord]) -> str:
    lines = [
        "# 项目接口对照",
        "",
        "| 符号 | 文档 | 项目使用位置 |",
        "| --- | --- | --- |",
    ]
    for symbol in PROJECT_HOTSPOTS:
        artifact = artifact_lookup.get(symbol)
        doc = "未抓取"
        if artifact:
            local = Path(artifact.local_path).relative_to(output_dir).as_posix()
            doc = f"[{artifact.title}]({local})"
        usages = find_usages(symbol, limit=4)
        usage_text = "<br>".join(
            f"`{item['path']}:{item['line']}`" for item in usages
        ) or "-"
        lines.append(f"| `{symbol}` | {doc} | {usage_text} |")
    lines.append("")
    return "\n".join(lines) + "\n"


def write_json(path: Path, payload: object) -> None:
    ensure_dir(path.parent)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


def build_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="构建海康官方文档索引")
    parser.add_argument("--output", default=str(DEFAULT_OUTPUT), help="输出目录")
    parser.add_argument("--scope", default="full", choices=["full"], help="构建范围")
    parser.add_argument("--refresh", action="store_true", help="重新下载官方 HTML/PDF")
    parser.add_argument("--offline", action="store_true", help="仅使用本地缓存")
    parser.add_argument("--setup-mineru", action="store_true", help="自动创建 Python 3.12 venv 并安装 MinerU")
    return parser.parse_args()


def main() -> int:
    args = build_args()
    output_dir = Path(args.output).resolve()
    ensure_dir(output_dir)
    cache_dir = ensure_dir(output_dir / "cache")
    ensure_dir(output_dir / "sdk" / "definitions")
    ensure_dir(output_dir / "sdk" / "structures")
    ensure_dir(output_dir / "superbrain")
    ensure_dir(output_dir / "network-camera")

    mineru_venv = ROOT / "tools" / ".venvs" / "mineru312"
    mineru_exe = detect_mineru_exe(mineru_venv)
    if args.setup_mineru:
        mineru_exe = setup_mineru(mineru_venv)

    artifacts: list[ArtifactRecord] = []
    artifact_lookup: dict[str, ArtifactRecord] = {}
    manifest_entries: list[dict[str, object]] = []

    functions, structures = discover_symbols()
    sdk_targets = [("definitions", symbol) for symbol in functions] + [("structures", symbol) for symbol in structures]
    for category, symbol in sdk_targets:
        source_url = f"https://open.hikvision.com/hardware/{category}/{symbol}.html"
        cache_path = cache_dir / "raw-html" / "sdk" / category / f"{symbol}.html"
        try:
            blob, origin = http_fetch(source_url, refresh=args.refresh, offline=args.offline, cache_path=cache_path)
        except (error.HTTPError, error.URLError, FileNotFoundError):
            continue
        text = decode_html_blob(blob)
        if "<title>" not in text.lower():
            continue
        title, markdown, summary = html_to_markdown_sdk(symbol, "sdk", source_url, text)
        doc_path = output_dir / "sdk" / category / f"{symbol}.md"
        write_markdown(doc_path, markdown)
        split_markdown_by_headings(doc_path, output_dir / "sdk" / "sections", symbol)
        record = ArtifactRecord(
            doc_type="sdk_html",
            category="sdk",
            title=title,
            source_url=source_url,
            local_path=str(doc_path),
            symbol_or_topic=symbol,
            keywords=[symbol, category, "海康设备网络SDK"],
            summary=summary,
            project_usage=find_usages(symbol),
            raw_cache_path=str(cache_path),
            fetched_at=utc_now(),
            sha256=sha256_bytes(blob),
        )
        artifacts.append(record)
        artifact_lookup[symbol] = record
        manifest_entries.append(
            {
                "symbol": symbol,
                "category": category,
                "source_url": source_url,
                "local_md_path": rel_path(doc_path),
                "raw_cache_path": rel_path(cache_path),
                "fetched_at": record.fetched_at,
                "sha256": record.sha256,
                "origin": origin,
            }
        )

    for source in HTML_SOURCES:
        cache_path = cache_dir / "raw-html" / source.category / f"{source.slug}.html"
        try:
            blob, origin = http_fetch(source.source_url, refresh=args.refresh, offline=args.offline, cache_path=cache_path)
        except (error.HTTPError, error.URLError, FileNotFoundError) as exc:
            print(f"[WARN] 无法抓取 HTML：{source.source_url} ({exc})")
            continue
        text = decode_html_blob(blob)
        if source.kind == "product_page":
            title, markdown, related_titles = html_to_markdown_product_page(source, text)
        else:
            title, markdown = html_to_markdown_partner_detail(source, text)
            related_titles = []
        doc_dir = output_dir / source.category / "pages"
        doc_path = doc_dir / f"{source.slug}.md"
        write_markdown(doc_path, markdown)
        record = ArtifactRecord(
            doc_type=f"html_{source.kind}",
            category=source.category,
            title=title,
            source_url=source.source_url,
            local_path=str(doc_path),
            symbol_or_topic=source.slug,
            keywords=source.keywords,
            summary=extract_meta_description(text) or extract_first_paragraph(text) or title,
            raw_cache_path=str(cache_path),
            fetched_at=utc_now(),
            sha256=sha256_bytes(blob),
            related_symbols=related_titles,
        )
        artifacts.append(record)
        artifact_lookup[source.slug] = record
        manifest_entries.append(
            {
                "symbol": source.slug,
                "category": source.category,
                "source_url": source.source_url,
                "local_md_path": rel_path(doc_path),
                "raw_cache_path": rel_path(cache_path),
                "fetched_at": record.fetched_at,
                "sha256": record.sha256,
                "origin": origin,
            }
        )

    for pdf_source in PDF_SOURCES:
        pdf_cache_path = cache_dir / "raw-pdf" / pdf_source.category / pdf_source.target_pdf_name
        try:
            blob, origin = http_fetch(
                pdf_source.source_url,
                refresh=args.refresh,
                offline=args.offline,
                cache_path=pdf_cache_path,
            )
        except (error.HTTPError, error.URLError, FileNotFoundError) as exc:
            print(f"[WARN] 无法抓取 PDF：{pdf_source.source_url} ({exc})")
            md_path = output_dir / pdf_source.category / "pages" / f"{pdf_source.slug}.md"
            lines = [
                f"# {pdf_source.title}",
                "",
                f"- 来源：[{pdf_source.source_url}]({pdf_source.source_url})",
                "",
                "## 抓取状态",
                "",
                f"- 当前无法直接下载官方 PDF：`{exc}`",
                "- 脚本已保留该官方地址，后续如网络策略或访问权限变化，可直接重跑构建自动补齐。",
                "",
            ]
            write_markdown(md_path, "\n".join(lines).strip() + "\n")
            record = ArtifactRecord(
                doc_type="pdf_unavailable",
                category=pdf_source.category,
                title=pdf_source.title,
                source_url=pdf_source.source_url,
                local_path=str(md_path),
                symbol_or_topic=pdf_source.slug,
                keywords=pdf_source.keywords,
                summary=f"{pdf_source.title} 的官方 PDF 地址已记录，但当前抓取受限。",
                fetched_at=utc_now(),
                notes=[str(exc)],
            )
            artifacts.append(record)
            artifact_lookup[pdf_source.slug] = record
            manifest_entries.append(
                {
                    "symbol": pdf_source.slug,
                    "category": pdf_source.category,
                    "source_url": pdf_source.source_url,
                    "local_md_path": rel_path(md_path),
                    "fetched_at": record.fetched_at,
                    "fetch_error": str(exc),
                }
            )
            continue

        pdf_visible_path = output_dir / pdf_source.category / "pdf" / pdf_source.target_pdf_name
        ensure_dir(pdf_visible_path.parent)
        shutil.copy2(pdf_cache_path, pdf_visible_path)

        manifest_entries.append(
            {
                "symbol": pdf_source.slug,
                "category": pdf_source.category,
                "source_url": pdf_source.source_url,
                "local_pdf_path": rel_path(pdf_visible_path),
                "raw_cache_path": rel_path(pdf_cache_path),
                "fetched_at": utc_now(),
                "sha256": sha256_bytes(blob),
                "origin": origin,
            }
        )

        lines = [
            f"# {pdf_source.title}",
            "",
            f"- 来源：[{pdf_source.source_url}]({pdf_source.source_url})",
            f"- 原始 PDF：[{pdf_visible_path.name}](../pdf/{parse.quote(pdf_visible_path.name)})",
            "",
        ]
        converted_dir = output_dir / pdf_source.category / "pdf-md" / pdf_source.slug
        sections_dir = output_dir / pdf_source.category / "sections"
        if mineru_exe:
            work_dir = output_dir / ".mineru-temp" / pdf_source.slug
            try:
                primary_md, md_files = convert_pdf_with_mineru(pdf_visible_path, mineru_exe, work_dir)
            except (subprocess.CalledProcessError, FileNotFoundError) as exc:
                lines.extend(
                    [
                        "## MinerU 转换状态",
                        "",
                        f"- 转换失败：`{exc}`",
                        "- 已保留原始 PDF，可在修复 MinerU 环境后重新执行构建。",
                        "",
                    ]
                )
                primary_md = None
                md_files = []
            if primary_md:
                ensure_dir(converted_dir)
                copied_primary = converted_dir / f"{pdf_source.slug}.md"
                copied_primary.write_text(primary_md.read_text(encoding="utf-8", errors="ignore"), encoding="utf-8")
                for asset in md_files:
                    if asset == primary_md:
                        continue
                    target = converted_dir / asset.name
                    copy_tree_if_exists(asset, target)
                created_sections = split_markdown_by_headings(copied_primary, sections_dir, pdf_source.slug)
                lines.extend(
                    [
                        "## MinerU 转换结果",
                        "",
                        f"- 主 Markdown：[{copied_primary.name}](../pdf-md/{pdf_source.slug}/{copied_primary.name})",
                    ]
                )
                if created_sections:
                    lines.append(f"- 拆分节数：{len(created_sections)}")
                lines.append("")
                summary = f"{pdf_source.title} 的官方 PDF，已通过 MinerU 转为 Markdown。"
            else:
                summary = f"{pdf_source.title} 的官方 PDF。"
        else:
            lines.extend(
                [
                    "## MinerU 转换状态",
                    "",
                    "- 当前未检测到 MinerU CLI。",
                    "- 可执行 `python tools/build_hikvision_docs.py --output \"设备网络SDK编程指南（明眸-以人为中心）\" --scope full --setup-mineru` 后重新构建。",
                    "",
                ]
            )
            summary = f"{pdf_source.title} 的官方 PDF。"

        md_path = output_dir / pdf_source.category / "pages" / f"{pdf_source.slug}.md"
        write_markdown(md_path, "\n".join(lines).strip() + "\n")
        record = ArtifactRecord(
            doc_type="pdf",
            category=pdf_source.category,
            title=pdf_source.title,
            source_url=pdf_source.source_url,
            local_path=str(md_path),
            symbol_or_topic=pdf_source.slug,
            keywords=pdf_source.keywords,
            summary=summary,
            raw_cache_path=str(pdf_cache_path),
            fetched_at=utc_now(),
            sha256=sha256_bytes(blob),
        )
        artifacts.append(record)
        artifact_lookup[pdf_source.slug] = record

    readme = render_root_readme(output_dir, artifacts)
    write_markdown(output_dir / "README.md", readme)
    write_markdown(output_dir / "技术规范.md", render_technical_spec(output_dir, artifact_lookup))
    write_markdown(output_dir / "目录索引.md", render_directory_index(output_dir, artifacts))
    write_markdown(output_dir / "项目接口对照.md", render_project_mapping(output_dir, artifact_lookup))
    write_json(
        output_dir / "ai-index.json",
        [
            {
                "doc_type": artifact.doc_type,
                "category": artifact.category,
                "title": artifact.title,
                "symbol_or_topic": artifact.symbol_or_topic,
                "keywords": artifact.keywords,
                "summary": artifact.summary,
                "source_url": artifact.source_url,
                "local_path": rel_path(Path(artifact.local_path)),
                "section_anchor": artifact.section_anchor,
                "project_usage": artifact.project_usage,
            }
            for artifact in artifacts
        ],
    )
    write_json(output_dir / "manifest.json", manifest_entries)
    print(f"[OK] 生成完成：{output_dir}")
    print(f"[OK] 文档总数：{len(artifacts)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
