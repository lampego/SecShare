FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install Node.js for the Web project's Tailwind build.
RUN apt-get update \
    && apt-get install -y --no-install-recommends apt-transport-https wget ca-certificates curl \
    && curl -fsSL https://deb.nodesource.com/setup_lts.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && node -v \
    && npm -v \
    && rm -rf /var/lib/apt/lists/*

# Copy all files
COPY . .

# Create dummy local configurations so build/restore/publish are happy
RUN echo "{}" > appsettings.Local.json
RUN echo "{}" > SecShare.Server/appsettings.Local.json
RUN echo "{}" > SecShare.Migrations/appsettings.Local.json
RUN echo "{}" > SecShare.WorkerServices/appsettings.Local.json
RUN echo "{}" > SecShare.Tests.Integration.Api/appsettings.Local.json
RUN echo "{}" > SecShare.Tests.Integration/appsettings.Local.json

# Publish API
RUN dotnet publish SecShare.Server/SecShare.Server.csproj -c Release -o /app/publish/api

# Runtime image with ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Install nginx for reverse proxy with rate limiting
RUN apt-get update && apt-get install -y --no-install-recommends nginx && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Change timezone to UTC
ENV TZ=UTC
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

# Copy built API files
COPY --from=build /app/publish/api /app/publish/api

# Copy nginx configuration (with rate limiting for file operations)
COPY devops/api/nginx.conf /etc/nginx/nginx.conf

# Copy boot script that starts both dotnet and nginx
COPY devops/api/boot.sh /app/publish/api/boot.sh
RUN chmod +x /app/publish/api/boot.sh

# Create dummy local config in the runtime directory
RUN echo "{}" > /app/publish/api/appsettings.Local.json

# ASP.NET Core listens on localhost:5000
ENV ASPNETCORE_URLS=http://127.0.0.1:5000

EXPOSE 80

# Set working directory to where wwwroot and app binaries are located
WORKDIR /app/publish/api

# Run boot script that starts both dotnet and nginx
CMD ["/bin/bash", "/app/publish/api/boot.sh"]
