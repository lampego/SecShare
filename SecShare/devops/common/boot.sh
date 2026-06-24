#!/bin/bash

echo "Start application.."
echo "Project dir is: $PROJECT_DIR"
cd "./$PROJECT_DIR"
dotnet run --no-launch-profile --configuration=Release --environment="$ASPNETCORE_ENVIRONMENT"

