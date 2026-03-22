FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BankInsight.API/BankInsight.API.csproj BankInsight.API/
COPY BankingOS/api/BankingOS.API.csproj BankingOS/api/
RUN dotnet restore BankingOS/api/BankingOS.API.csproj

COPY BankInsight.API/. BankInsight.API/
COPY BankingOS/. BankingOS/

WORKDIR /src/BankingOS/api
RUN dotnet publish BankingOS.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
COPY BankingOS /BankingOS
ENTRYPOINT ["dotnet", "BankingOS.API.dll"]
