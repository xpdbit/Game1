# Game1 PR 审查指南

> 所有 Pull Request 必须经过审查后方可合并至 main 分支

---

## 审查前置条件

PR 提交前，请确保：

- [ ] 代码已通过本地构建
- [ ] 所有单元测试通过
- [ ] 无静态分析 Critical/High 级别警告
- [ ] 代码风格符合 `.editorconfig` 规范
- [ ] 提交信息清晰描述变更内容

---

## 审查清单

### 1. 代码质量 (Code Quality)

- [ ] **无硬编码值**: 检查是否存在 MagicNumber、硬编码字符串等
  - 数值常量应定义命名常量
  - 字符串应使用 const 或资源文件
- [ ] **无空引用风险**: 检查是否存在可能的 NullReferenceException
  - 使用空值检查
  - 使用空值传播操作符 `?.`
  - 使用空合并操作符 `??`
- [ ] **无资源泄漏**: 检查 IDisposable、文件流、事件订阅等是否正确处理
- [ ] **无性能问题**: 检查是否存在不必要的循环、字符串拼接等

### 2. Unity 特定规范

- [ ] **API 使用正确**: 检查是否正确使用 Unity API
  - 在主线程调用 Unity API
  - 避免在 Update 中使用 FindObjectOfType
  - 正确使用 Invoke/StartCoroutine
- [ ] **MonoBehaviour 规范**:
  - 无空 MonoBehaviour
  - 正确使用 SerializeField
  - 避免 public 字段（使用属性或方法）
- [ ] **资源管理**: 检查资源加载和卸载是否配对

### 3. 架构与设计

- [ ] **单一职责**: 每个类/方法是否只有一个职责
- [ ] **模块解耦**: 模块间是否通过接口/事件通信，而非直接依赖
- [ ] **可测试性**: 业务逻辑是否独立于 MonoBehaviour

### 4. 代码风格

- [ ] **命名规范**: 符合项目约定（PascalCase、CamelCase）
- [ ] **注释完整**: 复杂逻辑有适当注释，公共API有文档注释
- [ ] **格式化**: 符合 `.editorconfig` 配置

### 5. 安全性

- [ ] **无敏感信息泄露**: API密钥、密码等不应硬编码
- [ ] **输入验证**: 用户输入是否经过验证

### 6. 测试覆盖（适用于功能变更）

- [ ] **新增功能有测试**: 核心逻辑有对应的单元测试
- [ ] **测试质量**: 测试是否为有效用例（非仅覆盖路径）

---

## 大型重构要求

对于以下情况的重构，需要额外文档：

- [ ] **架构变更**: 创建 `Docs/superpowers/specs/` 下的设计文档
- [ ] **API 变更**: 提供迁移指南
- [ ] **数据库变更**: 提供数据迁移脚本
- [ ] **配置文件变更**: 说明配置项用途

---

## 审查流程

```
1. 作者提交 PR
2. CI 检查:
   - 静态分析 (无 Critical/High 警告)
   - 代码风格检查
   - 单元测试
   - 构建验证
3. 至少 1 人 Review 批准
4. 作者处理反馈（如有）
5. 合并至 main
```

---

## Review 权限

| 角色 | 可合并 |
|------|--------|
| 维护者 (Maintainer) | ✅ 可合并 |
| 审核者 (Reviewer) | ✅ 批准后合并 |
| 贡献者 (Contributor) | 需要 1+ 审核 |

---

## 警告级别定义

| 级别 | 说明 | 处理要求 |
|------|------|----------|
| Critical | 可能导致崩溃、数据丢失、安全问题 | **必须修复** |
| High | 可能导致性能问题、逻辑错误 | **必须修复** |
| Medium | 代码可读性、可维护性问题 | 建议修复 |
| Low | 风格偏好、代码优化建议 | 可选修复 |

---

## 常见问题

### Q: 为什么我的 PR 被阻止合并？
A: 检查 CI 状态，确保所有检查通过：
- 静态分析无 Critical/High 警告
- 代码风格检查通过
- 构建成功
- 至少 1 人 approved

### Q: 如何处理争议性的审查意见？
A:
1. 在评论中讨论技术细节
2. 如无法达成共识，寻求第三方意见
3. 维护者有最终决定权

### Q: 可以先合并后修复吗？
A: **不允许**。所有 Critical/High 警告必须在合并前修复。

---

## 参考资料

- [C# 编码约定](https://docs.microsoft.com/zh-cn/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Unity 脚本规范](https://docs.unity3d.com/cn/current/Manual/ScriptingGuidelines.html)
- [Roslyn 分析器](https://docs.microsoft.com/zh-cn/dotnet/fundamentals/code-analysis/overview)
