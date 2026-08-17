FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_HTTP_PORTS=8080

COPY . .

USER $APP_UID

ENTRYPOINT ["dotnet", "API.LiveOps.dll"]
