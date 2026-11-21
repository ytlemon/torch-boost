# Torchlight: Infinite 优化工具

针对《火炬之光：无限》(Torchlight: Infinite) 的 Windows 一键优化工具，涵盖 **CPU / GPU / 内存 / 网络** 四大模块。

## 文件说明

| 文件 | 用途 |
|---|---|
| `火炬之光无限优化工具_Setup.exe` | **推荐**：自包含安装程序，复制到任意 Windows 电脑双击安装（含桌面/开始菜单快捷方式、卸载功能） |
| `火炬之光无限优化工具.exe` | 主程序单文件，可直接双击运行（无需安装） |
| `Optimizer.cs` / `Setup.cs` | C# 源码（Roslyn 编译） |

## 功能

- **CPU**：游戏进程优先级→高于正常、启用 Windows 游戏模式、高性能电源计划、清理占 CPU 后台进程
- **GPU**：硬件加速 GPU 调度(HAGS)、关闭全屏优化降低输入延迟、显卡电源→最高性能
- **内存**：清理所有进程工作集释放物理内存、关闭占内存后台程序
- **网络**：DNS 一键切换（阿里/腾讯/Google/Cloudflare）、TCP 参数优化、内置延迟测试
- **通用**：自动检测游戏安装（进程名+常见目录），支持手动指定路径；所有改动自动备份，一键恢复；设置记忆

## 使用方法

1. 运行 `火炬之光无限优化工具_Setup.exe` 安装，或直接双击 `火炬之光无限优化工具.exe`。
2. **建议右键 → 以管理员身份运行**（改注册表/电源计划需要管理员权限）。
3. 自动检测游戏；若未检测到，可先启动游戏再点“重新检测”，或在 `%APPDATA%\TLIOptimizer\game_path.txt` 写入游戏 exe 完整路径。
4. 勾选需要的优化项，点“一键优化”。
5. 不满意点“一键恢复”，所有改动记录在同目录 `backup.txt`。

> HAGS / DNS / 电源计划需重启电脑或重连网络才完全生效。

## 兼容 / 注意

- 适用 Windows 10/11（需 .NET Framework 4.x，系统自带）。
- 支持任意客户端（Steam / 国服 / Epic），工具通过进程名和路径自动检测。
- 修改系统级设置（注册表、DNS、电源）前会自动备份；恢复功能可还原。
- 本工具仅供个人合法使用。

## 源码编译（可选）

用 Roslyn csc 编译：

```
csc /target:winexe /out:火炬之光无限优化工具.exe ^
  /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
  /r:System.Management.dll /r:System.Core.dll Optimizer.cs

csc /target:winexe /out:Setup.exe /resource:火炬之光无限优化工具.exe,TLIOptimizerExe ^
  /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll Setup.cs
```
