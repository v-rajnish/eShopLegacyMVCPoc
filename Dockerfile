# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore (leverages layer caching)
COPY eShopPorted/eShopPorted.csproj eShopPorted/
COPY eShopLegacy.Utilities/eShopLegacy.Utilities.csproj eShopLegacy.Utilities/
RUN dotnet restore eShopPorted/eShopPorted.csproj

# Copy the rest of the source and publish
COPY eShopPorted/ eShopPorted/
COPY eShopLegacy.Utilities/ eShopLegacy.Utilities/
RUN dotnet publish eShopPorted/eShopPorted.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "eShopPorted.dll"]
