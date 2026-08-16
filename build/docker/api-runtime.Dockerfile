FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY api/publish/ .

EXPOSE 8080
ENTRYPOINT ["dotnet", "RentalManager.Api.dll"]
