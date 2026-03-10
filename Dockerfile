# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file
COPY ["src/MLN131.Api/MLN131.Api.csproj", "src/MLN131.Api/"]

# Restore dependencies
RUN dotnet restore "src/MLN131.Api/MLN131.Api.csproj"

# Copy all source code
COPY . .

# Build application
RUN dotnet build "src/MLN131.Api/MLN131.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "src/MLN131.Api/MLN131.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "MLN131.Api.dll"]
