FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app/publish

COPY . /app/publish
ENTRYPOINT ["dotnet", "Worker.LL.dll"]
