# Offline Insider Enroll UI

一个基于 **WinUI 3** 的现代图形界面工具，用于在无需登录微软账户的情况下，快速、安全地管理 Windows Insider 频道注册状态。

本项目基于 [OfflineInsiderEnroll](https://github.com/abbodi1406/offlineinsiderenroll) 实现，并提供更友好的可视化界面、自动化操作与系统信息展示。

---

## ✨ 功能特性

- 🖥 **图形界面操作**  
  无需命令行，一键切换 Insider 频道。

- 🔧 **支持所有 Insider 频道**
  - Canary Channel  
  - Dev Channel  
  - Beta Channel  
  - Release Preview Channel  

- 🔐 **无需微软账户**  
  直接修改系统配置，无需登录 Microsoft 账号。

- 📦 **安全的 WinUI 3 桌面应用**  
  使用 MSIX 打包，支持自动更新、沙盒隔离与干净卸载。

- 🖊 **完整的本地化支持**  
  - 简体中文（zh-CN）  
  - 英语（en-US）  
  自动随系统语言切换。

- 🖥 **系统信息展示**  
  显示 Windows 版本、内部版本号、修订号等详细信息。

---

## 📸 截图

> 以下为示例截图，你可以替换为实际图片。

| 主界面 | 设置页面 |
|-------|----------|
|<img width="1920" height="1080" alt="屏幕截图 2026-01-21 035049" src="https://github.com/user-attachments/assets/da51816f-8a2b-473f-aeca-2e650b36bcbd" />|<img width="1920" height="1080" alt="屏幕截图 2026-01-21 035116" src="https://github.com/user-attachments/assets/7b8fafde-01b8-4030-b496-1e2e09001a91" />|

---

## 📥 下载

你可以从以下位置下载最新版本：

👉 **[Microsoft Store（推荐）](https://apps.microsoft.com/)**  
👉 **[GitHub Releases](https://github.com/你的仓库/releases)**

MSIX 包支持：

- 自动更新  
- 干净卸载  
- x64 / ARM64 架构  

---

## 🛠 构建方式（Build）

本项目使用 **WinUI 3 + .NET 8 + Windows App SDK 1.8**。

### 1. 克隆仓库

```bash
git clone https://github.com/你的仓库.git
cd OfflineInsiderEnrollUI
