# Multi-stage build for AetherDesk SaaS Backend REST API
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

# Create volume/folder for local sqlite db storage if PostgreSQL is not used
RUN mkdir -p data

EXPOSE 5000

CMD ["node", "dist/server.js"]
