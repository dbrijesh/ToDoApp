# Use Node 20 Alpine as base image  
FROM node:20-alpine AS build

# Set working directory
WORKDIR /app

# Copy package.json and install dependencies
COPY package*.json ./
RUN npm install --legacy-peer-deps --no-fund --no-audit

# Copy source code
COPY . .

# Build the application
RUN npm run build

# Production stage - use nginx to serve
FROM nginx:alpine AS production

# Remove default nginx files and copy built application
RUN rm -rf /usr/share/nginx/html/*
COPY --from=build /app/dist/hello-world/browser /usr/share/nginx/html

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]