# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy contracts and project files
COPY contracts/grpc/user-profile/v1/ contracts/grpc/user-profile/v1/
COPY examples/grpc-demo/src/GrpcDemo.Contracts/ examples/grpc-demo/src/GrpcDemo.Contracts/
COPY examples/grpc-demo/src/ChatRestApi/ChatRestApi.csproj examples/grpc-demo/src/ChatRestApi/
RUN dotnet restore examples/grpc-demo/src/ChatRestApi/ChatRestApi.csproj

# Copy source and publish
COPY examples/grpc-demo/src/ChatRestApi/ examples/grpc-demo/src/ChatRestApi/
RUN dotnet publish examples/grpc-demo/src/ChatRestApi/ChatRestApi.csproj -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:5002
EXPOSE 5002
ENTRYPOINT ["dotnet", "ChatRestApi.dll"]
