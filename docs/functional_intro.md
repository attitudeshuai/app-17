# 功能说明文档

## 1. 业务背景与解决的问题

### 1.1 业务背景

在多代同堂家庭、慢性病患者家庭和合租群体中，家庭常备药品的管理一直是个容易被忽视的问题。常见痛点包括：

- **药品过期无人知晓**：药品购买后随意放置，过期后仍在使用，存在安全隐患
- **老人小孩误服风险**：药品存放位置不明确，禁忌说明不清晰，容易造成误服
- **重复购买浪费**：家庭成员之间不沟通，导致重复购买相同药品
- **用药记录缺失**：没有系统记录谁、什么时候、用了什么药，无法追溯
- **库存管理混乱**：不知道家里有什么药、有多少、放哪里，需要时找不到

### 1.2 解决的问题

MedCabinet（家庭药品共享柜）旨在解决以上问题，通过数字化方式管理家庭药箱：

1. **药品信息数字化**：统一录入所有药品的名称、适应症、用法用量、有效期等信息
2. **有效期预警**：临期和过期药品自动提醒，避免使用过期药品
3. **家庭成员共享**：全家人可以查看和管理同一药箱，信息实时同步
4. **用药记录追踪**：记录每次用药的人、时间、剂量、症状，便于健康管理
5. **库存智能管理**：库存不足自动提醒，避免重复购买或缺药

## 2. 用户角色与核心用例

### 2.1 用户角色

| 角色 | 权限说明 |
|------|----------|
| **Owner (所有者)** | 家庭创建者，拥有所有权限，可删除家庭、管理成员角色 |
| **Admin (管理员)** | 可管理药品、添加/移除成员（Owner除外）、管理用药记录 |
| **Member (成员)** | 可查看药品、添加用药记录、查看提醒 |

### 2.2 核心用例

#### 用例1：用户注册与登录
- **参与者**：新用户
- **前置条件**：用户未登录
- **主流程**：
  1. 用户输入用户名、邮箱、密码
  2. 系统验证信息唯一性
  3. 密码加密存储
  4. 创建用户账户并返回 JWT Token
- **后置条件**：用户成功注册/登录

#### 用例2：创建家庭
- **参与者**：注册用户
- **前置条件**：用户已登录
- **主流程**：
  1. 用户输入家庭名称
  2. 系统生成唯一邀请码
  3. 创建家庭记录
  4. 创建者自动成为 Owner 角色
- **后置条件**：家庭创建成功

#### 用例3：添加药品
- **参与者**：家庭成员（Owner/Admin）
- **前置条件**：用户已登录，且属于某个家庭
- **主流程**：
  1. 用户填写药品信息（名称、分类、有效期、库存等）
  2. 系统自动计算药品状态
  3. 保存药品记录
  4. 如果临期或库存不足，自动生成提醒
- **后置条件**：药品添加成功

#### 用例4：记录用药
- **参与者**：家庭成员
- **前置条件**：用户已登录，家庭中有药品
- **主流程**：
  1. 用户选择药品、填写用药人、剂量、症状
  2. 系统扣减药品库存
  3. 保存用药记录
  4. 库存不足时生成提醒
- **后置条件**：用药记录保存成功，库存已更新

#### 用例5：查看提醒
- **参与者**：家庭成员
- **前置条件**：用户已登录
- **主流程**：
  1. 用户进入提醒列表
  2. 系统展示该用户的所有提醒
  3. 用户可标记提醒为已读
- **后置条件**：用户查看了提醒信息

## 3. 功能模块详细说明

### 3.1 用户认证模块

**功能描述**：提供用户注册、登录、信息管理功能，使用 JWT Token 进行身份认证。

**主要功能**：
- 用户注册：用户名、邮箱、密码校验，密码 BCrypt 加密存储
- 用户登录：用户名+密码验证，返回 JWT Token
- 获取当前用户信息：从 Token 中解析用户 ID，返回用户详情
- 更新个人信息：修改用户名、邮箱、头像等

**安全机制**：
- 密码使用 BCrypt 算法加密
- JWT Token 有效期 24 小时
- 敏感接口需要 Bearer Token 认证

### 3.2 家庭管理模块

**功能描述**：管理家庭基本信息，支持创建、编辑、删除家庭。

**主要功能**：
- 家庭列表：分页查询用户加入的所有家庭
- 家庭详情：查看家庭基本信息、成员数量、药品数量
- 创建家庭：生成唯一邀请码，创建者自动成为 Owner
- 更新家庭：修改家庭名称等信息
- 删除家庭：仅 Owner 可删除，级联删除所有相关数据

**业务规则**：
- 每个家庭有唯一的邀请码
- 创建者自动成为 Owner
- 一个用户可以加入多个家庭

### 3.3 家庭成员管理模块

**功能描述**：管理家庭中的成员及其角色。

**主要功能**：
- 成员列表：查看家庭所有成员
- 添加成员：通过用户ID添加
- 邀请加入：通过邀请码加入家庭
- 更新成员角色：修改成员权限
- 移除成员：从家庭中移除某个成员
- 我的家庭：查看用户加入的所有家庭

**业务规则**：
- Owner 不能被移除
- Admin 不能移除其他 Admin
- 通过邀请码加入的用户默认角色为 Member
- 一个用户在一个家庭中只有一个角色

### 3.4 药品管理模块

**功能描述**：管理家庭中的药品信息。

**主要功能**：
- 药品列表：分页、搜索、筛选、排序
- 药品详情：查看药品完整信息
- 添加药品：录入药品信息，自动计算状态
- 更新药品：修改药品信息，重新计算状态
- 删除药品：删除药品记录
- 更新状态：手动修改药品状态

**业务规则**：
- 药品状态自动计算规则：
  - 库存 ≤ 0 → Empty
  - 有效期 ≤ 0 → Expired
  - 有效期 ≤ 30天且库存 > 0 → NearExpiry
  - 其他 → Valid
- 添加/更新药品时自动生成相关提醒

### 3.5 用药管理模块

**功能描述**：记录家庭成员的用药情况。

**主要功能**：
- 用药记录列表：按药品、时间、用户筛选
- 用药详情：查看单次用药的详细信息
- 新增用药：记录用药人、剂量、症状等
- 更新用药：修改用药记录，调整库存
- 删除用药：删除记录，归还库存
- 我的用药：查看当前用户的用药记录

**业务规则**：
- 新增用药自动扣减库存
- 删除用药自动归还库存
- 库存不足时不能添加用药记录
- 用药人可以不是系统用户（如老人小孩）

### 3.6 提醒管理模块

**功能描述**：管理药品相关的提醒通知。

**主要功能**：
- 提醒列表：按类型、状态筛选
- 提醒详情：查看提醒详细信息
- 创建提醒：手动创建提醒
- 更新提醒：标记已读、修改内容
- 删除提醒：删除提醒
- 我的提醒：查看当前用户的所有提醒

**自动提醒规则**：
- 药品临期（30天内）→ 生成 NearExpiry 提醒
- 药品过期 → 生成 Expired 提醒
- 库存 ≤ 5 → 生成 LowStock 提醒
- 库存 = 0 → 生成 EmptyStock 提醒
- 提醒会发送给家庭所有成员

### 3.7 统计与搜索模块

**功能描述**：提供数据统计和全局搜索功能。

**主要功能**：
- 总览统计：家庭数、成员数、药品数、用药记录数、提醒数
- 药品状态分布：有效/临期/过期/空库存 数量统计
- 分类统计：按药品分类统计数量
- 用药趋势：按月统计用药次数
- 全局搜索：搜索药品名称、适应症、存放位置等

## 4. 数据库 ER 图文字描述

### 4.1 表关系说明

```
Users (用户表)
  ├─ 1:N → HouseholdMembers (家庭成员表)
  ├─ 1:N → MedUsages (用药记录表)
  └─ 1:N → MedAlerts (药品提醒表)

Households (家庭表)
  ├─ 1:1 → Users (创建者)
  ├─ 1:N → HouseholdMembers (家庭成员)
  └─ 1:N → Medicines (药品)

HouseholdMembers (家庭成员表)
  ├─ N:1 → Households (所属家庭)
  └─ N:1 → Users (对应用户)

Medicines (药品表)
  ├─ N:1 → Households (所属家庭)
  ├─ 1:N → MedUsages (用药记录)
  └─ 1:N → MedAlerts (提醒)

MedUsages (用药记录表)
  ├─ N:1 → Medicines (对应药品)
  └─ N:1 → Users (操作人)

MedAlerts (药品提醒表)
  ├─ N:1 → Medicines (对应药品)
  └─ N:1 → Users (提醒对象)
```

### 4.2 表详情

**Users（用户表）**
- Id：主键，自增
- Username：用户名，唯一，长度 50
- Email：邮箱，唯一，长度 100
- PasswordHash：密码哈希，长度 255
- Avatar：头像 URL，可选，长度 500
- CreatedAt：创建时间，默认当前时间
- UpdatedAt：更新时间，默认当前时间

**Households（家庭表）**
- Id：主键，自增
- Name：家庭名称，长度 100
- InviteCode：邀请码，唯一，长度 20
- CreatedBy：创建者用户 ID，外键
- CreatedAt：创建时间，默认当前时间

**HouseholdMembers（家庭成员表）**
- Id：主键，自增
- HouseholdId：家庭 ID，外键
- UserId：用户 ID，外键
- Role：角色（Owner/Admin/Member），长度 20
- JoinedAt：加入时间，默认当前时间
- 联合唯一约束：(HouseholdId, UserId)

**Medicines（药品表）**
- Id：主键，自增
- HouseholdId：家庭 ID，外键
- Name：药品名称，长度 200
- Category：药品分类，长度 50
- Indication：适应症，长度 500
- Dosage：用法用量，长度 200
- ExpiryDate：有效期
- StockQuantity：库存数量
- StorageLocation：存放位置，长度 200
- Contraindications：禁忌说明，长度 1000，可选
- PhotoUrl：药品图片 URL，长度 500，可选
- Status：药品状态（Valid/NearExpiry/Expired/Empty）
- CreatedAt：创建时间，默认当前时间

**MedUsages（用药记录表）**
- Id：主键，自增
- MedicineId：药品 ID，外键
- UserId：操作人用户 ID，外键
- UsedBy：用药人，长度 50
- UsedQuantity：使用数量
- UsedAt：用药时间，默认当前时间
- SymptomNote：症状备注，长度 500，可选

**MedAlerts（药品提醒表）**
- Id：主键，自增
- MedicineId：药品 ID，外键
- UserId：用户 ID，外键
- AlertType：提醒类型（NearExpiry/Expired/LowStock/EmptyStock/UsageReminder）
- Message：提醒消息，长度 500
- IsRead：是否已读，默认 false
- CreatedAt：创建时间，默认当前时间

## 5. 关键业务规则

### 5.1 状态流转规则

**药品状态计算**：
```
if 库存 <= 0:
    状态 = Empty
elif 有效期 <= 今天:
    状态 = Expired
elif 有效期 - 今天 <= 30天:
    状态 = NearExpiry
else:
    状态 = Valid
```

**提醒生成规则**：
- 药品状态变更时自动检查
- 每个用户每种提醒类型只保留一条未读记录
- 库存变化时检查库存提醒
- 有效期检查每天定时执行（当前版本在创建/更新时触发）

### 5.2 权限规则

**家庭操作权限**：
- 查看：所有家庭成员
- 修改家庭信息：Owner、Admin
- 删除家庭：Owner

**成员管理权限**：
- 查看成员：所有家庭成员
- 添加成员：Owner、Admin
- 修改角色：Owner
- 移除成员：
  - Owner 可以移除任何人（除了自己）
  - Admin 可以移除 Member，不能移除 Owner 和其他 Admin
  - Member 不能移除任何人

**药品管理权限**：
- 查看：所有家庭成员
- 新增/修改/删除：Owner、Admin
- 修改状态：Owner、Admin

**用药记录权限**：
- 查看：所有家庭成员
- 新增：所有家庭成员
- 修改/删除：
  - 创建者可以修改/删除自己的记录
  - Owner、Admin 可以修改/删除任何人的记录

**提醒权限**：
- 查看：提醒所属用户、Owner、Admin
- 修改/删除：提醒所属用户、Owner、Admin

### 5.3 时间计算逻辑

**临期计算**：
- 临期阈值：30 天
- 计算方式：`有效期 - 当前时间 <= 30天`

**用药时间**：
- 未指定时默认使用当前时间
- 支持自定义用药时间（用于补录）

## 6. 接口调用示例

### 6.1 用户注册

**请求**：
```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "testuser",
  "email": "test@example.com",
  "password": "123456"
}
```

**响应**：
```json
{
  "code": 200,
  "message": "注册成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2024-01-02T00:00:00Z",
    "user": {
      "id": 6,
      "username": "testuser",
      "email": "test@example.com",
      "avatar": null,
      "createdAt": "2024-01-01T00:00:00Z"
    }
  }
}
```

### 6.2 用户登录

**请求**：
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "zhangsan",
  "password": "123456"
}
```

**响应**：
```json
{
  "code": 200,
  "message": "登录成功",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2024-01-02T00:00:00Z",
    "user": {
      "id": 1,
      "username": "zhangsan",
      "email": "zhangsan@example.com",
      "avatar": "https://api.dicebear.com/7.x/avataaars/svg?seed=zhangsan",
      "createdAt": "2024-01-01T00:00:00Z"
    }
  }
}
```

### 6.3 获取药品列表

**请求**：
```http
GET /api/medicines?pageIndex=1&pageSize=10&status=Valid
Authorization: Bearer {token}
```

**响应**：
```json
{
  "code": 200,
  "message": "操作成功",
  "data": {
    "items": [
      {
        "id": 1,
        "householdId": 1,
        "name": "布洛芬缓释胶囊",
        "category": "解热镇痛",
        "indication": "用于缓解轻至中度疼痛...",
        "dosage": "口服。成人，一次1粒，一日2次...",
        "expiryDate": "2024-07-01T00:00:00Z",
        "stockQuantity": 24,
        "storageLocation": "客厅药箱第一层",
        "contraindications": "对其他非甾体抗炎药过敏者禁用...",
        "photoUrl": "https://example.com/ibuprofen.jpg",
        "status": "Valid",
        "createdAt": "2024-01-01T00:00:00Z",
        "daysUntilExpiry": 180
      }
    ],
    "totalCount": 7,
    "pageIndex": 1,
    "pageSize": 10,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

### 6.4 创建用药记录

**请求**：
```http
POST /api/medusages
Content-Type: application/json
Authorization: Bearer {token}

{
  "medicineId": 1,
  "usedBy": "张三",
  "usedQuantity": 1,
  "symptomNote": "头痛"
}
```

**响应**：
```json
{
  "code": 200,
  "message": "创建成功",
  "data": {
    "id": 7,
    "medicineId": 1,
    "userId": 1,
    "usedBy": "张三",
    "usedQuantity": 1,
    "usedAt": "2024-01-15T10:00:00Z",
    "symptomNote": "头痛",
    "medicineName": "布洛芬缓释胶囊",
    "username": "zhangsan"
  }
}
```

### 6.5 获取统计总览

**请求**：
```http
GET /api/stats/overview
Authorization: Bearer {token}
```

**响应**：
```json
{
  "code": 200,
  "message": "操作成功",
  "data": {
    "totalHouseholds": 1,
    "totalMembers": 3,
    "totalMedicines": 7,
    "totalUsages": 4,
    "totalAlerts": 3,
    "expiredMedicines": 1,
    "nearExpiryMedicines": 1,
    "validMedicines": 5,
    "emptyMedicines": 0,
    "unreadAlerts": 3,
    "categoryStats": [
      { "category": "感冒用药", "count": 2 },
      { "category": "解热镇痛", "count": 1 }
    ],
    "monthlyUsageStats": [
      { "month": "2023-08", "usageCount": 0 },
      { "month": "2023-09", "usageCount": 1 }
    ]
  }
}
```
