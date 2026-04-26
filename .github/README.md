# Game1 CI/CD

GitHub Actions CI/CD流水线配置，用于Game1项目的自动化构建、测试和发布。

## 工作流

### CI (Pull Request)
- **触发条件**: PR到main分支
- **功能**:
  - Windows Standalone 64位构建
  - WebGL构建
  - 编辑器模式单元测试
  - 构建产物保存90天
  - 失败通知

### Release Build
- **触发条件**: 推送到main分支
- **功能**:
  - Windows Standalone 64位Release构建
  - WebGL Release构建
  - 编辑器模式单元测试
  - 构建产物保存90天
  - 成功/失败通知

## 必需Secrets配置

在GitHub仓库的 `Settings > Secrets and variables > Actions` 中添加以下Secrets：

| Secret名称 | 说明 | 获取方式 |
|-----------|------|---------|
| `UNITY_LICENSE` | Unity许可证 | [Unity官方](https://license.unity3d.com) |
| `UNITY_EMAIL` | Unity账户邮箱 | Unity ID邮箱 |
| `UNITY_PASSWORD` | Unity账户密码 | Unity ID密码 |
| `SLACK_WEBHOOK_URL` | Slack Webhook URL (可选) | Slack App配置 |

### 获取Unity许可证
1. 访问 [Unity License Management](https://license.unity3d.com)
2. 登录Unity ID账号
3. 下载许可证文件 (.ulf)
4. 将许可证内容复制为Secrets值

## 产物下载

构建完成后，访问Actions标签页下载对应构建产物：

- `Windows-Release` / `Windows-Build-{sha}`: Windows可执行文件
- `WebGL-Release` / `WebGL-Build-{sha}`: WebGL构建产物

## 构建日志

所有构建日志作为Artifact保存，可用于调试构建问题。

## 技术细节

- **Unity版本**: 6000.1.5f1
- **容器镜像**: gerardmorchilony/unity-ci-ubuntu-with-mutiplayer:v2.0
- **Artifact保留期**: 90天
