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
WORKDIR /app

# Change timezone to UTC
ENV TZ=UTC
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

# Copy built API files
COPY --from=build /app/publish/api /app/publish/api

# Create dummy local config in the runtime directory
RUN echo "{}" > /app/publish/api/appsettings.Local.json

ENV ASPNETCORE_URLS=http://+:80

EXPOSE 80

CMD ["dotnet", "/app/publish/api/SecShare.Server.dll"]
