# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy contracts, data and project files
COPY contracts/ contracts/
COPY data/rag/ data/rag/
COPY src/AiService.Repositories/AiService.Repositories.csproj src/AiService.Repositories/
COPY src/AiService.Services/AiService.Services.csproj src/AiService.Services/
COPY src/AiService.Api/AiService.Api.csproj src/AiService.Api/

RUN dotnet restore src/AiService.Api/AiService.Api.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/AiService.Api/AiService.Api.csproj -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
COPY data/rag/ data/rag/

ENV ASPNETCORE_URLS=http://+:5004
EXPOSE 5004

ENTRYPOINT ["dotnet", "AiService.Api.dll"]

