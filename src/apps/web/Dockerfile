FROM node:20-alpine AS build
WORKDIR /app

COPY src/apps/web/package*.json ./
RUN npm ci --no-audit --no-fund || npm install --no-audit --no-fund

COPY src/apps/web/. .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY src/apps/web/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 5173 80

CMD ["nginx", "-g", "daemon off;"]
