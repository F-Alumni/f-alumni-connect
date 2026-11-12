FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY contracts/ contracts/
COPY src/NotificationService.Repositories/NotificationService.Repositories.csproj src/NotificationService.Repositories/
COPY src/NotificationService.Services/NotificationService.Services.csproj src/NotificationService.Services/
COPY src/NotificationService.Api/NotificationService.Api.csproj src/NotificationService.Api/

RUN dotnet restore src/NotificationService.Api/NotificationService.Api.csproj

COPY src/ src/
RUN dotnet publish src/NotificationService.Api/NotificationService.Api.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

ENV ASPNETCORE_URLS=http://+:5006
EXPOSE 5006

ENTRYPOINT ["dotnet", "NotificationService.Api.dll"]
