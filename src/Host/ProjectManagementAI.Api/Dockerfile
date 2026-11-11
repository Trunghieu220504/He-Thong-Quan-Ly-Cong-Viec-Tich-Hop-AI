FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore src/Host/ProjectManagementAI.Api/ProjectManagementAI.Api.csproj
RUN dotnet publish src/Host/ProjectManagementAI.Api/ProjectManagementAI.Api.csproj -c Release --no-restore -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=5000
USER $APP_UID
EXPOSE 5000
HEALTHCHECK --interval=10s --timeout=5s --retries=5 --start-period=30s CMD bash -c 'echo > /dev/tcp/127.0.0.1/5000'
ENTRYPOINT ["dotnet", "ProjectManagementAI.Api.dll"]
