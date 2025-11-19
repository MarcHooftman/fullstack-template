# Use the official .NET 9 SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY Api/*.csproj Api/
# COPY Api.Tests/*.csproj Api.Tests/
RUN dotnet restore Api/Api.csproj

# Copy the rest of the source code
COPY Api/. Api/
WORKDIR /src/Api

# Build and publish the application
RUN dotnet publish -c Release -o /app/publish

# Use the official .NET 9 ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 80
EXPOSE 80

# Start the application
ENTRYPOINT ["dotnet", "Api.dll"]