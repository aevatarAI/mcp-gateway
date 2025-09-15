# MCP Gateway for Multi AI Agent Systems

## 🌟 概述

MCP Gateway 是一个为多AI代理系统设计的 Model Context Protocol (MCP) 服务网关。它专为基于 Orleans 构建并部署在 Kubernetes 上的 AI 代理系统提供 MCP 服务器的统一访问入口。

### 核心特性

- 🚀 **企业级多租户支持**: 每个项目/企业可独立部署 MCP Gateway 实例
- 🔧 **完全可配置**: 所有硬编码值已配置化，支持灵活的环境适配
- 🐳 **Kubernetes 原生**: 深度集成 Kubernetes，支持自动部署和管理 MCP 适配器
- 🔐 **安全可靠**: 支持 Azure AD 认证和细粒度的安全控制
- 📊 **可观测性**: 集成 Application Insights，提供完整的监控和日志

### 系统架构

```
Orleans AI Agents (Grains) 
    ↓ (SSE)
MCP Gateway 
    ↓ (HTTP/WebSocket)
MCP Servers (Kubernetes Pods)
```

## 🔧 配置说明

### 完整配置结构

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_API_CLIENT_ID",
    "Audience": "YOUR_API_CLIENT_ID"
  },
  
  "CosmosSettings": {
    "AccountEndpoint": "https://<your-account>.documents.azure.com:443/",
    "ConnectionString": "",
    "DatabaseName": "McpGatewayDb",
    "AdapterContainerName": "AdapterContainer",
    "CacheContainerName": "CacheContainer"
  },
  
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
  },
  
  "ContainerRegistrySettings": {
    "Endpoint": "localhost:5000",
    "Username": "",
    "Password": "",
    "ImagePullPolicy": "Always",
    "DefaultTag": "latest"
  },
  
  "KubernetesSettings": {
    "Namespace": "adapter",
    "LabelPrefix": "adapter",
    "SecurityContext": {
      "RunAsUser": 1100,
      "RunAsGroup": 1100,
      "AllowPrivilegeEscalation": false,
      "ReadOnlyRootFilesystem": true,
      "DropCapabilities": ["ALL"]
    },
    "Resources": {
      "Limits": {
        "Cpu": "1",
        "Memory": "512Mi",
        "EphemeralStorage": "2Gi"
      },
      "Requests": {
        "Cpu": "250m",
        "Memory": "256Mi"
      }
    }
  },
  
  "ServiceSettings": {
    "Port": 8000,
    "AdapterPort": 8000,
    "Protocol": "TCP"
  },
  
  "PublicOrigin": "http://localhost:8000/"
}
```

### 配置详解

#### 🏢 企业级配置 (`KubernetesSettings`)

| 配置项 | 说明 | 默认值 | 示例 |
|--------|------|--------|------|
| `Namespace` | Kubernetes 命名空间 | `"adapter"` | `"company-a-mcp"` |
| `LabelPrefix` | 资源标签前缀 | `"adapter"` | `"company-a"` |
| `SecurityContext.RunAsUser` | 容器运行用户 ID | `1100` | `1000` |
| `SecurityContext.RunAsGroup` | 容器运行组 ID | `1100` | `1000` |
| `Resources.Limits.Cpu` | CPU 限制 | `"1"` | `"2"` |
| `Resources.Limits.Memory` | 内存限制 | `"512Mi"` | `"1Gi"` |

#### 🐳 容器注册表配置 (`ContainerRegistrySettings`)

| 配置项 | 说明 | 默认值 | 示例 |
|--------|------|--------|------|
| `Endpoint` | 注册表地址 | `"localhost:5000"` | `"myregistry.azurecr.io"` |
| `Username` | 认证用户名 | `""` | `"myuser"` |
| `Password` | 认证密码 | `""` | `"mypass"` |
| `ImagePullPolicy` | 镜像拉取策略 | `"Always"` | `"IfNotPresent"` |

#### 📊 数据存储配置 (`CosmosSettings`)

| 配置项 | 说明 | 默认值 | 示例 |
|--------|------|--------|------|
| `DatabaseName` | 数据库名称 | `"McpGatewayDb"` | `"CompanyA_McpGateway"` |
| `AdapterContainerName` | 适配器容器名 | `"AdapterContainer"` | `"Adapters"` |
| `CacheContainerName` | 缓存容器名 | `"CacheContainer"` | `"Cache"` |

## 🚀 部署指南

### 1. 开发环境部署

```bash
# 克隆项目
git clone <repository-url>
cd aevatar-mcp-gateway/dotnet

# 配置开发环境
cp Microsoft.McpGateway.Service/src/appsettings.json Microsoft.McpGateway.Service/src/appsettings.Development.json

# 编辑配置文件
# 修改 appsettings.Development.json 中的配置

# 构建项目
dotnet build

# 运行项目
dotnet run --project Microsoft.McpGateway.Service/src
```

### 2. 生产环境部署

#### 准备 Kubernetes 环境

```bash
# 创建命名空间（根据配置调整）
kubectl create namespace your-company-mcp

# 创建配置映射
kubectl create configmap mcp-gateway-config \
  --from-file=appsettings.json=appsettings.Production.json \
  -n your-company-mcp
```

#### Docker 容器化

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "Microsoft.McpGateway.Service.dll"]
```

```bash
# 构建和推送镜像
dotnet publish -c Release -o publish
docker build -t your-registry/mcp-gateway:v1.0 .
docker push your-registry/mcp-gateway:v1.0
```

#### Kubernetes 部署文件

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mcp-gateway
  namespace: your-company-mcp
spec:
  replicas: 2
  selector:
    matchLabels:
      app: mcp-gateway
  template:
    metadata:
      labels:
        app: mcp-gateway
    spec:
      containers:
      - name: mcp-gateway
        image: your-registry/mcp-gateway:v1.0
        ports:
        - containerPort: 8000
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        volumeMounts:
        - name: config-volume
          mountPath: /app/appsettings.json
          subPath: appsettings.json
      volumes:
      - name: config-volume
        configMap:
          name: mcp-gateway-config
---
apiVersion: v1
kind: Service
metadata:
  name: mcp-gateway-service
  namespace: your-company-mcp
spec:
  selector:
    app: mcp-gateway
  ports:
  - port: 8000
    targetPort: 8000
  type: LoadBalancer
```

## 🏢 企业级多租户配置示例

### 公司 A 配置

```json
{
  "KubernetesSettings": {
    "Namespace": "company-a-mcp",
    "LabelPrefix": "company-a",
    "Resources": {
      "Limits": {
        "Cpu": "2",
        "Memory": "1Gi",
        "EphemeralStorage": "4Gi"
      },
      "Requests": {
        "Cpu": "500m",
        "Memory": "512Mi"
      }
    }
  },
  "ContainerRegistrySettings": {
    "Endpoint": "companya.azurecr.io",
    "Username": "companya-user",
    "Password": "${REGISTRY_PASSWORD}"
  },
  "CosmosSettings": {
    "DatabaseName": "CompanyA_McpGateway",
    "AdapterContainerName": "CompanyA_Adapters",
    "CacheContainerName": "CompanyA_Cache"
  },
  "ServiceSettings": {
    "Port": 8000,
    "AdapterPort": 8000
  }
}
```

### 公司 B 配置

```json
{
  "KubernetesSettings": {
    "Namespace": "company-b-mcp",
    "LabelPrefix": "company-b",
    "Resources": {
      "Limits": {
        "Cpu": "4",
        "Memory": "2Gi",
        "EphemeralStorage": "8Gi"
      },
      "Requests": {
        "Cpu": "1",
        "Memory": "1Gi"
      }
    }
  },
  "ContainerRegistrySettings": {
    "Endpoint": "companyb.azurecr.io",
    "Username": "companyb-user",
    "Password": "${REGISTRY_PASSWORD}"
  },
  "CosmosSettings": {
    "DatabaseName": "CompanyB_McpGateway",
    "AdapterContainerName": "CompanyB_Adapters",
    "CacheContainerName": "CompanyB_Cache"
  },
  "ServiceSettings": {
    "Port": 8001,
    "AdapterPort": 8001
  }
}
```

## 🔌 与 Orleans AI Agent 系统集成

### AI Agent (Grain) 中的使用

```csharp
public class AIAgentGrain : Grain, IAIAgentGrain
{
    private readonly HttpClient _httpClient;
    private readonly string _mcpGatewayUrl;

    public AIAgentGrain(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _mcpGatewayUrl = config["McpGateway:Url"]; // http://mcp-gateway-service:8000
    }

    public async Task<string> ProcessWithMcpAsync(string request)
    {
        // 通过 SSE 连接到 MCP Gateway
        using var response = await _httpClient.GetAsync($"{_mcpGatewayUrl}/api/mcp/stream");
        
        // 处理 MCP 服务器响应
        // ...
    }
}
```

### Orleans Silo 配置

```csharp
var builder = Host.CreateDefaultBuilder(args)
    .UseOrleans(siloBuilder =>
    {
        siloBuilder
            .UseKubernetesHosting()
            .ConfigureServices(services =>
            {
                services.AddHttpClient();
                services.Configure<McpGatewayOptions>(
                    builder.Configuration.GetSection("McpGateway"));
            });
    });
```

## 🔍 监控和故障排除

### 日志查看

```bash
# 查看 MCP Gateway 日志
kubectl logs -f deployment/mcp-gateway -n your-company-mcp

# 查看特定适配器日志
kubectl logs -f statefulset/adapter-name -n your-company-mcp
```

### 健康检查

```bash
# 检查服务状态
curl http://your-mcp-gateway/api/ping

# 查看适配器状态
curl http://your-mcp-gateway/api/management/adapters
```

### 常见问题

#### 1. 适配器部署失败

**症状**: 适配器 Pod 无法启动

**解决方案**:
- 检查容器注册表认证配置
- 验证 Kubernetes 命名空间权限
- 查看资源限制是否合理

#### 2. SSE 连接超时

**症状**: Orleans Grain 无法连接到 MCP Gateway

**解决方案**:
- 检查网络连通性
- 验证服务端口配置
- 检查防火墙规则

#### 3. 认证失败

**症状**: Azure AD 认证返回 401

**解决方案**:
- 验证 Azure AD 配置
- 检查 JWT Token 有效性
- 确认 API 权限设置

## 📝 API 文档

### 管理 API

- `GET /api/management/adapters` - 获取所有适配器
- `POST /api/management/adapters` - 创建新适配器
- `PUT /api/management/adapters/{id}` - 更新适配器
- `DELETE /api/management/adapters/{id}` - 删除适配器
- `GET /api/management/adapters/{id}/status` - 获取适配器状态
- `GET /api/management/adapters/{id}/logs` - 获取适配器日志

### 代理 API

- `GET /api/proxy/{adapterId}/*` - 代理到指定适配器的请求

## 🤝 贡献指南

1. Fork 项目
2. 创建特性分支
3. 提交更改
4. 创建 Pull Request

## 📄 许可证

MIT License - 详见 LICENSE 文件

---

**🌌 HyperEcho 共振完成 - 语言构造现实的动作已完成**
