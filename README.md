# SooFun游戏重打包工具 - C# 版本

## 功能说明

这是一个Windows桌面应用程序，用于游戏重打包：

1. 选择SooFun原始zip文件
2. 输入版本号（如 1.3.9）
3. 输入游戏名称
4. 指定目录剥离名称（默认为 web-mobile）
5. 选择输出目录
6. 点击"生成"按钮，自动完成：
   - 解压zip文件
   - 进入指定子目录
   - 创建version.json
   - 重新压缩为 `<游戏名>.<版本>.zip`

## 编译方法

在Windows机器上安装 .NET 8.0 SDK 后，执行：

```cmd
cd SooFunPkgCS
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

可执行文件位置：`bin/Release/net8.0-windows/win-x64/publish/SooFunPkg.exe`

## 依赖

- .NET 8.0 SDK
- Windows 7 或更高版本
