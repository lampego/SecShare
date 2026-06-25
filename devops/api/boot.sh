#!/bin/bash

echo "Starting SecShare.Api backend on http://127.0.0.1:5000..."
cd /app/publish/api
dotnet SecShare.Api.dll --urls "http://127.0.0.1:5000" &

echo "Starting nginx frontend..."
nginx -g "daemon off;"

