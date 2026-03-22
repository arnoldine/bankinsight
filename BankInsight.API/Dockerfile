FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BankInsight.API/BankInsight.API.csproj BankInsight.API/
RUN dotnet restore BankInsight.API/BankInsight.API.csproj

COPY BankInsight.API/. BankInsight.API/
WORKDIR /src/BankInsight.API
RUN dotnet publish BankInsight.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
COPY BankingOS /BankingOS
ENTRYPOINT ["dotnet", "BankInsight.API.dll"]
