#!/bin/sh
rm -rf Redouble.AspNet.Webpack/bin
dotnet restore Redouble.AspNet.Webpack
dotnet build Redouble.AspNet.Webpack
dotnet pack Redouble.AspNet.Webpack --configuration Release