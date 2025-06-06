FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app/publish
EXPOSE 80
EXPOSE 443

ENV ASPNETCORE_HTTP_PORTS="80"

COPY . /app/publish
ENTRYPOINT ["dotnet", "API.Chat.dll"]