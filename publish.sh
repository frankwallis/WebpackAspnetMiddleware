#!/bin/sh
rm -rf bin
dotnet restore
dotnet build Redouble.AspNet.Webpack
dotnet pack Redouble.AspNet.Webpack --configuration Release