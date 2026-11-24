# Panel compilado para la DEMO local (docker-compose.demo.yml): nginx sirve dist/ y reenvía /api a la API.
FROM node:24-alpine AS compilar
WORKDIR /src
COPY package.json package-lock.json ./
COPY frontend/package.json frontend/
COPY packages/tokens/package.json packages/tokens/
COPY packages/ui/package.json packages/ui/
RUN npm ci --ignore-scripts
COPY tsconfig.base.json tsconfig.json ./
COPY packages/ packages/
COPY frontend/ frontend/
RUN npm run build -w frontend

FROM nginx:1.29-alpine
COPY demo/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=compilar /src/frontend/dist /usr/share/nginx/html
EXPOSE 80
