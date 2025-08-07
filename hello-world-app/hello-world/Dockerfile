# Use Node 20 Alpine as base image
FROM node:20-alpine

# Set working directory
WORKDIR /app

# Copy package files and install dependencies
COPY package.json ./
RUN npm install -g @angular/cli && npm install

# Copy source code
COPY . .

# Copy SSL certificates
ARG SSL_CERT_PATH=
ARG SSL_KEY_PATH=
RUN mkdir -p /app/ssl
COPY ${SSL_CERT_PATH} /app/ssl/server.cert.pem
COPY ${SSL_KEY_PATH} /app/ssl/server.key.pem

# Build the application
RUN npm run build

# Set environment variables
ENV PORT=4000
ENV SSL_ENABLED=true

# Expose port and start server
EXPOSE 4000
CMD ["npm", "run", "serve"]