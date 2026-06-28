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
RUN echo "{}" > SecShare.Api/appsettings.Local.json
RUN echo "{}" > SecShare.Migrations/appsettings.Local.json
RUN echo "{}" > SecShare.WorkerServices/appsettings.Local.json
RUN echo "{}" > SecShare.Tests.Integration.Api/appsettings.Local.json
RUN echo "{}" > SecShare.Tests.Integration/appsettings.Local.json

# Publish Web (Blazor WASM)
RUN dotnet publish SecShare.Web/SecShare.Web.csproj -c Release -o /app/publish/web

# Publish API
RUN dotnet publish SecShare.Api/SecShare.Api.csproj -c Release -o /app/publish/api

# Runtime image with ASP.NET and Nginx
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Change timezone to UTC
ENV TZ=UTC
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

RUN apt-get update && apt-get install -y nginx && rm -rf /var/lib/apt/lists/*

# Copy built API files and compiled Web static files
COPY --from=build /app/publish/api /app/publish/api
COPY --from=build /app/publish/web/wwwroot /var/www/wwwroot

# Create dummy local config in the runtime directory
RUN echo "{}" > /app/publish/api/appsettings.Local.json

# Copy nginx config and runtime boot script
COPY ./devops/api/nginx.conf /etc/nginx/nginx.conf
COPY ./devops/api/boot.sh /usr/local/bin/boot.sh
RUN chmod +x /usr/local/bin/boot.sh

EXPOSE 80

CMD ["sh", "/usr/local/bin/boot.sh"]
