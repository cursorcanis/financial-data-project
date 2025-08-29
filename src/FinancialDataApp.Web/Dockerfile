# Build SPA
FROM node:20-alpine AS build
WORKDIR /app

# Install deps using clean CI install
COPY src/FinancialDataApp.Web/package*.json ./
RUN npm ci

# Copy source and build
COPY src/FinancialDataApp.Web/ ./
RUN npm run build

# Minimal runner to expose built assets for nginx via a shared volume
FROM alpine:3.19 AS final
WORKDIR /app

# Copy build artifacts
COPY --from=build /app/dist /app/dist

# By default, copy built SPA to the mounted volume (/web-dist) and keep container alive.
# docker-compose overrides this command to the same effect.
CMD ["sh", "-c", "cp -r /app/dist/* /web-dist && echo 'SPA copied to volume' && tail -f /dev/null"]
