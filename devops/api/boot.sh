#!/bin/bash
set -e

echo "Starting SecShare.Server backend..."
cd /app/publish/api
dotnet SecShare.Server.dll &

echo "Starting nginx frontend..."
nginx -g "daemon off;"

