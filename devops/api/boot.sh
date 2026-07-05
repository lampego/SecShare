#!/bin/bash

echo "Starting SecShare.Server backend on http://127.0.0.1:5000..."
cd /app/publish/api
dotnet SecShare.Server.dll --urls "http://127.0.0.1:5000" &

echo "Starting nginx frontend..."
nginx -g "daemon off;"

