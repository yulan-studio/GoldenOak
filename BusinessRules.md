# GoldenOak Business Rules

本文件是 GoldenOak 项目的核心业务规则单一记录来源。凡是涉及租约、房源、租客、业主、权限或状态流转的核心逻辑变更，都必须同时更新代码和本文件。

## 租约状态流转

### 允许的状态变化

| 当前状态 | 允许变更为 |
|---|---|
| `PENDING` | `ACTIVE` |
| `PENDING` | `CANCELLED` |
| `ACTIVE` | `ENDED` |
| `ACTIVE` | `TERMINATED` |

### 禁止的状态变化

- `ENDED` 不允许重新变为 `ACTIVE`。
- `CANCELLED` 不允许重新变为 `ACTIVE`。
- `TERMINATED` 不允许重新变为 `ACTIVE`。
- 未明确列在允许列表中的状态变化一律拒绝。

## 租约与房源状态同步

- 租约变为 `ACTIVE` 时，对应房源的 `Unit.Status` 必须变为 `OCCUPIED`。
- 租约变为 `ENDED`、`TERMINATED` 或 `CANCELLED` 时，检查同一房源是否仍有其他 `ACTIVE` 租约。
  - 如果没有其他 `ACTIVE` 租约，房源状态变为 `AVAILABLE`。
  - 如果仍有其他 `ACTIVE` 租约，房源保持 `OCCUPIED`。
- 同一房源不能同时存在两个 `ACTIVE` 租约。
- 创建未来开始的租约时，房源可以标记为 `RESERVED`；租约开始后成为 `ACTIVE` 时标记为 `OCCUPIED`。

## 租约数据校验

- 租约开始日期不能晚于结束日期。
- 月租金不能小于 `0`。
- 押金不能小于 `0`。
- 创建租约时，房源必须存在。
- 同一个租客不能重复添加到同一租约。
- 一个租约最多只能有一个主租客；设置新的主租客时，原主租客自动取消主租客标记。

## 权限规则

- 未登录用户不能访问租约页面。
- `ADMIN` 和 `PROPERTY_MANAGER` 可以创建、编辑租约、修改状态及管理租客。
- 普通 `RENTER` 只能查看自己参与的租约，不能修改租约或租客关系。

## 维护约定

当新增或修改核心业务规则时：

1. 先确认规则和代码行为一致。
2. 更新本文件对应章节。
3. 同步更新相关服务、Controller、ViewModel 和测试。
4. 至少执行一次项目编译或相关测试。

