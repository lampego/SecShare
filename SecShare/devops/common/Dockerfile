FROM mcr.microsoft.com/dotnet/sdk:10.0

ARG ENVIRONMENT=Production

# Change timezone to UTC
ENV TZ=UTC
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

COPY . /app
WORKDIR /app/

ENV ASPNETCORE_ENVIRONMENT=${ENVIRONMENT}
ENV DOTNET_USE_POLLING_FILE_WATCHER=true  
ENV ASPNETCORE_URLS=http://+:80  
EXPOSE 80

RUN apt update
RUN apt install -y apt-transport-https wget ca-certificates curl

RUN echo "{}" > appsettings.Local.json
RUN echo "{}" > SecShare.Api/appsettings.Local.json
RUN echo "{}" > SecShare.Migrations/appsettings.Local.json
RUN echo "{}" > SecShare.WorkerServices/appsettings.Local.json
RUN echo "{}" > SecShare.Tests.Integration.Api/appsettings.Local.json
RUN echo "{}" > SecShare.Tests.Integration/appsettings.Local.json

RUN ["dotnet", "build", "--verbosity=q", "SecShare.Migrations/SecShare.Migrations.csproj"]
RUN ["dotnet", "build", "--verbosity=q", "SecShare.WorkerServices/SecShare.WorkerServices.csproj"]

COPY ./devops/common/boot.sh /usr/local/bin/php_boot

CMD ["sh", "/usr/local/bin/php_boot"]

