# ==============================
# BUILD STAGE
# ==============================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy everything to the container
COPY . .

# Restore and build
RUN dotnet restore
RUN dotnet publish -c Release -o out

# ==============================
# RUNTIME STAGE
# ==============================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy the published output from build stage
COPY --from=build /app/out .

# Expose port 8080 for web traffic
EXPOSE 8080

# Run your app
ENTRYPOINT ["dotnet", "ChatBoard.dll"]
