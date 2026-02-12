#!/bin/bash

pkill -f "Fatturazione.Api" || true
dotnet build
dotnet run --project src/Fatturazione.Api &
sleep 2

open http://localhost:5298


