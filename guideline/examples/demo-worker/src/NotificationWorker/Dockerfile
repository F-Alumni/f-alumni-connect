# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy contracts and project files
COPY contracts/events/ contracts/events/
COPY examples/demo-worker/src/NotificationWorker/NotificationWorker.csproj examples/demo-worker/src/NotificationWorker/
RUN dotnet restore examples/demo-worker/src/NotificationWorker/NotificationWorker.csproj

# Copy source and publish
COPY examples/demo-worker/src/NotificationWorker/ examples/demo-worker/src/NotificationWorker/
RUN dotnet publish examples/demo-worker/src/NotificationWorker/NotificationWorker.csproj -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
ENTRYPOINT ["dotnet", "NotificationWorker.dll"]
