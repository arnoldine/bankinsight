FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BankInsight.API/BankInsight.API.csproj BankInsight.API/
COPY Modules/Fintech/src/HybridTransfer.Domain/HybridTransfer.Domain.csproj Modules/Fintech/src/HybridTransfer.Domain/
COPY Modules/Fintech/src/HybridTransfer.Application/HybridTransfer.Application.csproj Modules/Fintech/src/HybridTransfer.Application/
COPY Modules/Fintech/src/HybridTransfer.Infrastructure/HybridTransfer.Infrastructure.csproj Modules/Fintech/src/HybridTransfer.Infrastructure/
RUN dotnet restore BankInsight.API/BankInsight.API.csproj

COPY BankInsight.API/. BankInsight.API/
COPY Modules/Fintech/. Modules/Fintech/
WORKDIR /src/BankInsight.API
RUN dotnet publish BankInsight.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
COPY BankingOS /BankingOS
ENTRYPOINT ["dotnet", "BankInsight.API.dll"]
