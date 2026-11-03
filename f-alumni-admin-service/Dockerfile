FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY contracts/ contracts/
COPY ["AdminService.sln", "./"]
COPY ["src/AdminService.Api/AdminService.Api.csproj", "src/AdminService.Api/"]
COPY ["src/AdminService.Services/AdminService.Services.csproj", "src/AdminService.Services/"]
COPY ["src/AdminService.Repositories/AdminService.Repositories.csproj", "src/AdminService.Repositories/"]
COPY ["tests/AdminService.Tests/AdminService.Tests.csproj", "tests/AdminService.Tests/"]

RUN dotnet restore "src/AdminService.Api/AdminService.Api.csproj"

COPY . .
WORKDIR "/src/src/AdminService.Api"
RUN dotnet publish "AdminService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5005
ENV ASPNETCORE_URLS=http://+:5005
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AdminService.Api.dll"]
