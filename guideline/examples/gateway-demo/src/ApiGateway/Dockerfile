# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy and restore project
COPY examples/gateway-demo/src/ApiGateway/ApiGateway.csproj examples/gateway-demo/src/ApiGateway/
RUN dotnet restore examples/gateway-demo/src/ApiGateway/ApiGateway.csproj

# Copy source files and publish release
COPY examples/gateway-demo/src/ApiGateway/ examples/gateway-demo/src/ApiGateway/
RUN dotnet publish examples/gateway-demo/src/ApiGateway/ApiGateway.csproj -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
