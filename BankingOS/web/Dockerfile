FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY src ./src
COPY BankingOS/web ./BankingOS/web

WORKDIR /app/BankingOS/web
ARG VITE_API_URL=/api
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY BankingOS/web/docker/nginx/default.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/BankingOS/web/dist /usr/share/nginx/html
EXPOSE 80
