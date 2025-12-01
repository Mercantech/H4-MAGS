# Stage 1: Build Flutter web app
FROM ghcr.io/cirruslabs/flutter:stable AS build-env

# Set working directory
WORKDIR /app

# Copy pubspec files
COPY pubspec.* ./

# Get dependencies
RUN flutter pub get

# Copy source code (this layer will be invalidated when source changes)
COPY lib/ ./lib/
COPY web/ ./web/
COPY assets/ ./assets/

# Copy other necessary files
COPY analysis_options.yaml ./

# Build for web
RUN flutter build web --release

# Stage 2: Create the run-time image
FROM nginx:alpine

# Copy the built web app
COPY --from=build-env /app/build/web /usr/share/nginx/html

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
