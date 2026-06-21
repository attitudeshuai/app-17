# MedCabinet - 家庭药品共享柜

面向家庭成员共享家庭常备药品的小系统。记录药品名称、适应症、有效期、存放位置与使用说明，在药品临期或重复购买时提醒，并支持家庭成员间用药咨询。

## 功能亮点

- 🏠 **家庭共享药箱**：支持多家庭管理，成员间共享药品信息
- ⏰ **智能提醒**：药品临期、过期、库存不足自动提醒
- 📊 **数据统计**：药品分类统计、用药趋势分析
- 🔐 **权限控制**：Owner/Admin/Member 三级角色权限管理
- 💊 **用药记录**：记录每位家庭成员的用药情况
- 📱 **RESTful API**：标准化接口设计，便于前端对接

## 技术栈

- **后端框架**: .NET Core 8.0 (ASP.NET Core Web API)
- **数据库**: MySQL 8.0
- **ORM**: Entity Framework Core 8.0 (Pomelo.EntityFrameworkCore.MySql)
- **认证**: JWT Bearer Token
- **API文档**: Swagger / OpenAPI
- **容器化**: Docker + Docker Compose
- **对象映射**: Mapster
- **参数校验**: FluentValidation
- **密码加密**: BCrypt.Net-Next

## 目录结构

```
MedCabinet/
├── src/
│   ├── MedCabinet.Api/              # API层（控制器、中间件）
│   │   ├── Controllers/              # API控制器
│   │   ├── Middleware/               # 中间件
│   │   ├── Program.cs                # 程序入口
│   │   └── appsettings.json          # 配置文件
│   ├── MedCabinet.Application/       # 应用层（服务、DTO、验证器）
│   │   ├── DTOs/                     # 数据传输对象
│   │   ├── Interfaces/               # 服务接口
│   │   ├── Services/                 # 服务实现
│   │   ├── Validators/               # 数据验证器
│   │   ├── Mappings/                 # 对象映射配置
│   │   └── DependencyInjection.cs    # 依赖注入扩展
│   ├── MedCabinet.Domain/            # 领域层（实体、接口）
│   │   ├── Entities/                 # 实体类
│   │   ├── Enums/                    # 枚举
│   │   └── Interfaces/               # 仓储接口
│   └── MedCabinet.Infrastructure/    # 基础设施层
│       ├── Data/                     # 数据库上下文
│       ├── Repositories/             # 仓储实现
│       └── DependencyInjection.cs    # 依赖注入扩展
├── tests/
│   └── MedCabinet.Tests/             # 单元测试项目
├── docs/
│   └── functional_intro.md           # 功能说明文档
├── postman_collection.json           # Postman测试集合
├── Dockerfile                        # Docker镜像构建
├── docker-compose.yml                # Docker编排配置
├── .gitignore                        # Git忽略文件
└── MedCabinet.sln                    # 解决方案文件
```

## 快速启动

### 前置要求

- Docker
- Docker Compose

### 启动步骤

1. **克隆并进入项目目录**：
   ```bash
   git clone <repo-url>
   cd MedCabinet
   ```

2. **Docker 启动服务**：
   ```bash
   docker-compose up --build -d
   ```

3. **查看日志**：
   ```bash
   docker-compose logs -f app
   ```

4. **验证服务健康**：
   ```bash
   curl http://localhost:8087/health
   ```

5. **访问 Swagger 文档**：
   - 打开浏览器访问 `http://localhost:8087/swagger`

6. **停止服务**：
   ```bash
   docker-compose down -v
   ```

### 默认测试账号

系统启动后会自动初始化测试数据，以下是默认账号：

| 用户名 | 密码 | 角色 |
|--------|------|------|
| zhangsan | 123456 | 家庭所有者 |
| lisi | 123456 | 家庭所有者 |
| wangwu | 123456 | 家庭成员 |
| zhaoliu | 123456 | 家庭管理员 |
| qianqi | 123456 | 家庭成员 |

## API 模块

### Auth（认证）
- `POST /api/auth/register` - 用户注册
- `POST /api/auth/login` - 用户登录
- `GET /api/auth/me` - 获取当前用户信息
- `PUT /api/auth/me` - 更新个人信息

### Households（家庭）
- `GET /api/households` - 获取家庭列表
- `POST /api/households` - 创建家庭
- `GET /api/households/{id}` - 获取家庭详情
- `PUT /api/households/{id}` - 更新家庭
- `DELETE /api/households/{id}` - 删除家庭

### HouseholdMembers（家庭成员）
- `GET /api/householdmembers` - 获取成员列表
- `POST /api/householdmembers` - 添加成员
- `POST /api/householdmembers/join` - 通过邀请码加入家庭
- `GET /api/householdmembers/{id}` - 获取成员详情
- `GET /api/householdmembers/mine` - 获取我所在的家庭
- `PUT /api/householdmembers/{id}` - 更新成员信息
- `DELETE /api/householdmembers/{id}` - 移除成员

### Medicines（药品）
- `GET /api/medicines` - 获取药品列表
- `POST /api/medicines` - 添加药品
- `GET /api/medicines/{id}` - 获取药品详情
- `PUT /api/medicines/{id}` - 更新药品
- `DELETE /api/medicines/{id}` - 删除药品
- `PATCH /api/medicines/{id}/status` - 更新药品状态

### MedUsages（用药记录）
- `GET /api/medusages` - 获取用药记录列表
- `POST /api/medusages` - 新增用药记录
- `GET /api/medusages/{id}` - 获取用药记录详情
- `GET /api/medusages/mine` - 获取我的用药记录
- `PUT /api/medusages/{id}` - 更新用药记录
- `DELETE /api/medusages/{id}` - 删除用药记录

### MedAlerts（提醒）
- `GET /api/medalerts` - 获取提醒列表
- `POST /api/medalerts` - 创建提醒
- `GET /api/medalerts/{id}` - 获取提醒详情
- `GET /api/medalerts/mine` - 获取我的提醒
- `PUT /api/medalerts/{id}` - 更新提醒
- `DELETE /api/medalerts/{id}` - 删除提醒

### Statistics（统计）
- `GET /api/stats/overview` - 总览统计
- `GET /api/stats/trend` - 趋势统计

## 测试

### Postman 测试集合

1. 打开 Postman
2. 导入 `postman_collection.json` 文件
3. 配置 `baseUrl` 环境变量为 `http://localhost:8087`
4. 执行测试集合

### 单元测试

```bash
dotnet test
```

## 数据库管理

可以通过 Adminer 管理数据库：
- 地址: `http://localhost:8080`
- 系统: MySQL
- 服务器: mysql
- 用户名: app_user
- 密码: app_pass
- 数据库: medcabinet

## 业务规则

### 角色权限
- **Owner (所有者)**: 拥有所有权限，可删除家庭
- **Admin (管理员)**: 可管理药品、成员、用药记录
- **Member (成员)**: 可查看药品、添加用药记录

### 药品状态流转
- **Valid (有效)**: 有效期 > 30天 且 库存 > 0
- **NearExpiry (临期)**: 有效期 ≤ 30天 且 库存 > 0
- **Expired (过期)**: 有效期 ≤ 0
- **Empty (库存为空)**: 库存 ≤ 0

### 自动提醒
- 药品临期（30天内）自动生成提醒
- 药品过期自动生成提醒
- 库存 ≤ 5 时生成低库存提醒
- 库存为 0 时生成空库存提醒

## 许可证

MIT License
