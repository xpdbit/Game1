# xNode 安装指南

## UPM 安装方式

### 方式一：直接从 Git 添加（推荐）

1. 打开 Unity 编辑器
2. 菜单栏选择 `Window` → `Package Manager`
3. 点击左上角 `+` 按钮
4. 选择 `Add package from git URL...`
5. 输入以下 URL：

```
https://github.com/Siccity/xNode.git#upm
```

6. 点击 `Add` 等待安装完成

### 方式二：OpenUPM

如果已安装 OpenUPM CLI，可使用命令：

```bash
openupm add com.github.siccity.xnode
```

### 方式三：手动修改 manifest.json

1. 找到项目中的 `Packages/manifest.json` 文件
2. 在 `dependencies` 中添加：

```json
{
  "dependencies": {
    "com.github.siccity.xnode": "https://github.com/Siccity/xNode.git#upm",
    ...
  }
}
```

3. 保存后 Unity 会自动解析包

## 验证安装

安装完成后，在代码中引用命名空间：

```csharp
using XNode;
```

如果无报错，说明安装成功。

## 资源链接

- GitHub 仓库：https://github.com/Siccity/xNode
- 文档：https://github.com/Siccity/xNode/wiki
