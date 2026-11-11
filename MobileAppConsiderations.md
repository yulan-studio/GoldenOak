# Mobile App Development Considerations

本文档记录 GoldenOak 目前开发时需要遵守的设计原则，以便未来可以较低成本地开发原生 iOS 或 Android App。

目标不是现在立即开发原生 App，而是保证现有网页系统的业务逻辑、数据结构和接口边界适合未来复用。

## 1. 分层原则

推荐保持以下结构：

```text
Web MVC Controller ─┐
                    ├── Shared Service ─── ApplicationDbContext
Mobile API Controller ─┘
```

- Razor MVC Controller 负责网页请求、ViewModel、页面跳转和网页错误显示。
- 将来新增的 API Controller 负责 JSON 请求和响应。
- Service 层负责核心业务规则，网页和手机端共同调用。
- 数据库查询和保存集中在 Service 或专门的数据访问层。
- 不要把核心业务规则只写在 Razor View 或网页 Controller 中。

## 2. 业务规则必须放在 Service 层

以下逻辑必须可以被网页和手机端共同复用：

- 租约状态流转
- 房源状态同步
- 租客和租约关联
- 权限相关的业务检查
- 金额、日期和数据完整性校验
- 防止重复记录

Controller 可以做页面流程编排，但不能成为唯一的业务规则入口。

## 3. 使用 ViewModel 和 DTO

- 网页表单使用专用 ViewModel，不要直接绑定数据库实体。
- API 使用专用 Request DTO 和 Response DTO。
- 不要把 `CreatedAt`、`UpdatedAt`、系统状态等受保护字段直接暴露给客户端修改。
- 数据库实体不应该直接作为公开 API 响应模型。
- DTO 的字段名称和格式应保持稳定，避免手机端依赖数据库实体结构。

## 4. API 设计准备

未来增加 API 时，建议使用清晰的资源路径：

```text
GET    /api/tenancies
GET    /api/tenancies/{id}
POST   /api/tenancies
PUT    /api/tenancies/{id}
POST   /api/tenancies/{id}/status
GET    /api/tenants
POST   /api/tenants
```

- API 应返回稳定的 HTTP 状态码和错误结构。
- 列表接口应支持分页、搜索、排序和筛选。
- 不要让手机端依赖网页 View 的 HTML 结构。
- 对可能变化的 API 考虑版本控制，例如 `/api/v1/tenancies`。

## 5. 验证和错误处理

- 客户端验证只用于改善用户体验。
- Service 和 API 必须再次执行服务端验证。
- 错误信息应包含稳定的错误代码，文字可以根据语言本地化。
- 不要让手机端通过解析网页错误文字来判断失败原因。

建议未来使用类似结构：

```json
{
  "code": "ACTIVE_TENANCY_EXISTS",
  "message": "A different active tenancy already exists for this unit."
}
```

## 6. 身份验证和权限

- 网页继续使用 Cookie Authentication。
- 原生 App 应使用适合移动端的 Token/OAuth 认证方式。
- Service 层仍然要检查资源访问权限，不能只依赖网页上的按钮隐藏。
- 普通租客只能访问自己的租约和资料。
- ADMIN、PROPERTY_MANAGER 等角色规则需要保持统一。

## 7. 数据和时间处理

- 数据库时间统一使用 UTC 保存。
- API 日期格式保持明确，建议使用 ISO 8601。
- 金额使用 decimal，不要使用 double。
- DateOnly 和 DateTime 的用途要区分清楚。
- 手机端显示时间时，由客户端或 API 根据用户时区转换。

## 8. 租客和租约模块的具体准备

- `TenancyService` 和 `RenterService` 作为网页与移动端共享的业务入口。
- 租客创建、编辑、归档、删除规则不能只放在网页 Controller。
- 租约状态变化必须继续通过 Service 执行。
- 租约详情页的弹窗只是网页交互方式，未来 App 应通过 API 完成同样的 Service 操作。
- 租客搜索应使用独立的搜索服务或 API，不要依赖网页中的 JavaScript DOM 查询。

## 9. 文件和上传功能

未来如果加入租约文件、签署文件或图片上传：

- 文件保存逻辑放在独立的文件服务中。
- 数据库只保存文件 ID、路径、类型、大小和上传时间等元数据。
- API 不应直接暴露服务器物理路径。
- 考虑文件大小限制、权限、病毒扫描和下载授权。

## 10. 离线和网络不稳定准备

如果未来 App 需要部分离线功能：

- 列表读取接口考虑缓存和分页。
- 写入操作需要明确的成功、失败和重试状态。
- 关键业务写入需要幂等性，避免重复提交造成重复租约或重复付款。
- 不要假设一次网络请求一定成功。

## 11. 开发检查清单

每次新增核心功能时，检查以下问题：

- 核心规则是否放在 Service 层？
- 是否直接把数据库实体绑定到表单？
- 将来是否可以用 API DTO 表达同一功能？
- Controller 是否只负责请求编排和页面响应？
- 是否有稳定的错误代码或错误类型？
- 是否检查了权限和资源所有权？
- 日期、时间和金额是否使用正确类型？
- 是否需要分页、搜索或移动端友好的响应结构？
- 是否同步更新 `BusinessRules.md`？

## 12. 当前项目约定

未来开发网页功能时，默认遵循以下约定：

1. 业务规则写入 Service，并同步更新业务规则文档。
2. 网页表单使用专用 ViewModel。
3. 不让手机端必须复用 Razor HTML。
4. 不把业务判断写在 JavaScript 中作为唯一保护。
5. 需要跨客户端复用的操作，优先设计为清晰的 Service 方法。

