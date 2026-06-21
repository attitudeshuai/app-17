# 阶段1：构建
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制所有项目文件
COPY ["src/MedCabinet.Api/MedCabinet.Api.csproj", "src/MedCabinet.Api/"]
COPY ["src/MedCabinet.Application/MedCabinet.Application.csproj", "src/MedCabinet.Application/"]
COPY ["src/MedCabinet.Domain/MedCabinet.Domain.csproj", "src/MedCabinet.Domain/"]
COPY ["src/MedCabinet.Infrastructure/MedCabinet.Infrastructure.csproj", "src/MedCabinet.Infrastructure/"]

# 还原依赖
RUN dotnet restore "src/MedCabinet.Api/MedCabinet.Api.csproj"

# 复制所有源代码
COPY . .

# 构建并发布
WORKDIR "/src/src/MedCabinet.Api"
RUN dotnet publish "MedCabinet.Api.csproj" -c Release -o /app/publish

# 阶段2：运行
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# 暴露端口
EXPOSE 8087
ENV ASPNETCORE_URLS=http://+:8087

# 健康检查
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8087/health || exit 1

# 入口点
ENTRYPOINT ["dotnet", "MedCabinet.Api.dll"]
