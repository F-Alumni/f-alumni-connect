# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy contracts and project files
COPY contracts/ contracts/
COPY src/UserService.Repositories/UserService.Repositories.csproj src/UserService.Repositories/
COPY src/UserService.Services/UserService.Services.csproj src/UserService.Services/
COPY src/UserService.Api/UserService.Api.csproj src/UserService.Api/

RUN dotnet restore src/UserService.Api/UserService.Api.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/UserService.Api/UserService.Api.csproj -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

# Default to port 10000 for Render (or override via ASPNETCORE_URLS / PORT)
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
EXPOSE 5001
EXPOSE 50051

ENTRYPOINT ["dotnet", "UserService.Api.dll"]
