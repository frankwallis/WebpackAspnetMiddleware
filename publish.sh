#!/bin/sh
rm -rf Redouble.AspNet.Webpack/bin
dotnet restore
dotnet build Redouble.AspNet.Webpack
dotnet pack Redouble.AspNet.Webpack --configuration Release