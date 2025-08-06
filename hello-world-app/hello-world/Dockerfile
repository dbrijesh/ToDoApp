# Use Node 20 Alpine as build stage
FROM node:20-alpine AS build

# Install OpenSSL for certificate generation
RUN apk add --no-cache openssl

# Set working directory
WORKDIR /app

# Copy package.json and install dependencies
COPY package*.json ./
RUN npm install --legacy-peer-deps --no-fund --no-audit

# Copy source code and build scripts
COPY . .

# Generate SSL certificate inline
RUN mkdir -p /app/ssl && \
    openssl genrsa -out /app/ssl/server.key 2048 && \
    openssl req -new -key /app/ssl/server.key -out /app/ssl/server.csr -subj "/C=US/ST=CA/L=San Francisco/O=MyOrg/OU=IT/CN=localhost" && \
    openssl x509 -req -days 365 -in /app/ssl/server.csr -signkey /app/ssl/server.key -out /app/ssl/server.crt && \
    chmod 600 /app/ssl/server.key && \
    chmod 644 /app/ssl/server.crt

# Build the application for production
RUN npm run build

# Production stage - use nginx with SSL support
FROM nginx:alpine AS production

# Install OpenSSL (already included in nginx:alpine)
# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Remove default nginx files and copy built application
RUN rm -rf /usr/share/nginx/html/*
COPY --from=build /app/dist/hello-world/browser /usr/share/nginx/html

# Copy SSL certificates
COPY --from=build /app/ssl /app/ssl

# Set environment variables with defaults
ENV APP_NAME="Hello World App"
ENV APP_VERSION="1.0.0"
ENV SSL_ENABLED="true"
ENV SSL_CERT_VALIDITY_DAYS="365"
ENV SSL_CERT_COUNTRY="US"
ENV SSL_CERT_STATE="CA"
ENV SSL_CERT_CITY="San Francisco"
ENV SSL_CERT_ORG="MyOrg"
ENV SSL_CERT_OU="IT"
ENV SSL_CERT_CN="localhost"
ENV SERVER_PORT="443"
ENV SERVER_HOST="localhost"
ENV LOG_LEVEL="info"
ENV ENABLE_HEALTH_CHECK="true"
ENV ENABLE_METRICS="false"

# Expose both HTTP and HTTPS ports
EXPOSE 80 443

# Add health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

# Start nginx
CMD ["nginx", "-g", "daemon off;"]