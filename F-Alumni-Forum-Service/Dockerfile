# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy contracts and project files
COPY contracts/ contracts/
COPY src/ForumService.Repositories/ForumService.Repositories.csproj src/ForumService.Repositories/
COPY src/ForumService.Services/ForumService.Services.csproj src/ForumService.Services/
COPY src/ForumService.Api/ForumService.Api.csproj src/ForumService.Api/

RUN dotnet restore src/ForumService.Api/ForumService.Api.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/ForumService.Api/ForumService.Api.csproj -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

ENV ASPNETCORE_URLS=http://+:5003
EXPOSE 5003

ENTRYPOINT ["dotnet", "ForumService.Api.dll"]

