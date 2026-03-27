FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG API_BASE_URL=/

COPY NuGet.Config ./
COPY CoreBanker/CoreBanker.csproj CoreBanker/
RUN dotnet restore CoreBanker/CoreBanker.csproj --configfile NuGet.Config

COPY CoreBanker/. CoreBanker/
RUN printf '{\n  "ApiBaseUrl": "%s"\n}\n' "$API_BASE_URL" > CoreBanker/wwwroot/appsettings.Production.json
RUN dotnet publish CoreBanker/CoreBanker.csproj -c Release -o /app/publish --configfile NuGet.Config

FROM nginx:1.27-alpine AS runtime
WORKDIR /usr/share/nginx/html

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot ./

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
