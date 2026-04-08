FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json .
RUN npm ci
COPY . .
RUN npm run build

FROM node:20-alpine
RUN npm install -g serve
COPY --from=build /app/dist/teetime-automator-web /app
CMD ["sh", "-c", "serve -s /app -l ${PORT:-3000}"]
