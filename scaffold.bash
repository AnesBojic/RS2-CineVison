#!/usr/bin/env bash

set -e

SOLUTION_NAME="eCommerce"

echo "Creating solution folder..."
mkdir $SOLUTION_NAME
cd $SOLUTION_NAME

echo "Creating solution..."
dotnet new sln -n $SOLUTION_NAME

echo "Creating projects..."

# Web API
dotnet new webapi -n eCommerce.WebAPI --use-controllers

# Services layer
dotnet new classlib -n eCommerce.Services

# Model layer
dotnet new classlib -n eCommerce.Model

echo "Adding projects to solution..."
dotnet sln add eCommerce.WebAPI/eCommerce.WebAPI.csproj
dotnet sln add eCommerce.Services/eCommerce.Services.csproj
dotnet sln add eCommerce.Model/eCommerce.Model.csproj

echo "Adding project references..."

# WebAPI → Services
dotnet add eCommerce.WebAPI reference eCommerce.Services

# Services → Model
dotnet add eCommerce.Services reference eCommerce.Model

echo "Adding EF Core packages..."

# ---- EF Core in Services (DbContext lives here) ----
dotnet add eCommerce.Services package Microsoft.EntityFrameworkCore
dotnet add eCommerce.Services package Microsoft.EntityFrameworkCore.SqlServer

# ---- EF Core Design tools in WebAPI (for migrations) ----
dotnet add eCommerce.WebAPI package Microsoft.EntityFrameworkCore.Design

echo "Adding Scalar (OpenAPI UI) to WebAPI..."

dotnet add eCommerce.WebAPI package Scalar.AspNetCore

echo "Creating recommended folders..."

mkdir -p eCommerce.WebAPI/Controllers
mkdir -p eCommerce.WebAPI/Middlewares
mkdir -p eCommerce.WebAPI/Extensions

mkdir -p eCommerce.Services/Interfaces
mkdir -p eCommerce.Services/Implementations
mkdir -p eCommerce.Services/Database


mkdir -p eCommerce.Model/Responses
mkdir -p eCommerce.Model/Requests
mkdir -p eCommerce.Model/SearchObjects
mkdir -p eCommerce.Model/Enums

echo "Done!"
echo "Next steps:"
echo "1) Add DbContext in eCommerce.Services/Data"
echo "2) Register DbContext in WebAPI Program.cs"
echo "3) Configure Scalar in WebAPI"

echo "Open eCommerce2.sln in your IDE."
