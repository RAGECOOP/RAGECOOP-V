# Use the official image as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY RageCoop.Server/*.csproj ./RageCoop.Server/
COPY libs/*.dll ./libs/

# Assuming RageCoop.Core is a dependency, if not, you can comment out the next line
COPY RageCoop.Core/*.csproj ./RageCoop.Core/

RUN dotnet restore RageCoop.Server/RageCoop.Server.csproj

# Copy everything else and build
COPY . .
WORKDIR /src/RageCoop.Server
RUN dotnet publish -c Release -o /app

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=build-env /app .
ENTRYPOINT ["dotnet", "RageCoop.Server.dll"]
