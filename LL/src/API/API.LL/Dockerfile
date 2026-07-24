FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/API/API.LL/API.LL.csproj", "src/API/API.LL/"]
RUN dotnet restore "./src/API/API.LL/./API.LL.csproj"
COPY . .
WORKDIR "/src/src/API/API.LL"
RUN dotnet build "./API.LL.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./API.LL.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app/publish
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "API.LL.dll"]
