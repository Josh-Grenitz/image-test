FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . "AdminDashboardService"

RUN dotnet restore AdminDashboardService/AdminDashboardService.sln
WORKDIR "/src/AdminDashboardService"
#COPY . .

RUN dotnet build "AdminDashboardService.sln" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AdminDashboardService.sln" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT="Development"
ENTRYPOINT ["dotnet", "AdminDashboardService.dll"]
