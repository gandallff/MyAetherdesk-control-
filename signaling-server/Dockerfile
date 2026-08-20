# Multi-stage build for AetherDesk WebSocket Signaling Server
FROM node:20-alpine AS builder

WORKDIR /usr/src/app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

# Production runtime image
FROM node:20-alpine AS runner

WORKDIR /usr/src/app
ENV NODE_ENV=production

COPY package*.json ./
RUN npm ci --only=production

# Copy compiled code from builder
COPY --from=builder /usr/src/app/dist ./dist

EXPOSE 8080

CMD ["node", "dist/server.js"]
