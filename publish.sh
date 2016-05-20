#!/bin/sh
rm -rf bin
dotnet restore
dotnet build
dotnet pack --configuration Release
dotnet publish --configuration Release