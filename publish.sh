#!/bin/sh
rm -rf bin
dnu restore
dnu build
dnu pack --configuration Release
dnu publish --configuration Release