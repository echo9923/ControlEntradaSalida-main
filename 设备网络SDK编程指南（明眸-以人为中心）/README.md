# 海康官方开发文档恢复包

本目录用于补齐当前项目缺失的海康官方开发文档，并为 AI 检索提供拆分后的 Markdown、小文档和索引。

## 内容概览

- `sdk/`：设备网络 SDK / ISAPI 文档，共 126 项。
- `superbrain/`：超脑 / iDS / NVR 页面与资料详情，共 4 项。
- `network-camera/`：网络摄像头、Web 预览与视频侧文档，共 6 项。
- `cache/`：原始 HTML / PDF 缓存，不建议纳入版本控制。

## 使用方式

- 从 [目录索引.md](目录索引.md) 进入分类目录。
- 从 [项目接口对照.md](项目接口对照.md) 按当前项目里实际调用的接口跳转。
- 从 `ai-index.json` 做程序化检索。

## 构建命令

```powershell
python tools/build_hikvision_docs.py --output "设备网络SDK编程指南（明眸-以人为中心）" --scope full
```

如需自动安装 MinerU，可额外带上 `--setup-mineru`。
