# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy contracts and project files
COPY contracts/grpc/user-profile/v1/ contracts/grpc/user-profile/v1/
COPY examples/grpc-demo/src/GrpcDemo.Contracts/ examples/grpc-demo/src/GrpcDemo.Contracts/
COPY examples/grpc-demo/src/UserGrpcServer/UserGrpcServer.csproj examples/grpc-demo/src/UserGrpcServer/
RUN dotnet restore examples/grpc-demo/src/UserGrpcServer/UserGrpcServer.csproj

# Copy source and publish
COPY examples/grpc-demo/src/UserGrpcServer/ examples/grpc-demo/src/UserGrpcServer/
RUN dotnet publish examples/grpc-demo/src/UserGrpcServer/UserGrpcServer.csproj -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:50051
ENV Kestrel__Endpoints__gRpc__Url=http://+:50051
ENV Kestrel__Endpoints__gRpc__Protocols=Http2
EXPOSE 50051
ENTRYPOINT ["dotnet", "UserGrpcServer.dll"]
