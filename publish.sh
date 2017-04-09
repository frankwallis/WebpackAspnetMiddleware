#!/bin/sh
rm -rf Redouble.AspNet.Webpack/bin
dotnet restore
dotnet build
dotnet pack Redouble.AspNet.Webpack --configuration Release
dotnet nuget push ./Redouble.AspNet.Webpack/bin/Release/Redouble.AspNet.Webpack.*.nupkg