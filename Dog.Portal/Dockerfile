FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY Dog.Application/Dog.Application.csproj Dog.Application/
COPY Dog.Portal/Dog.Portal.csproj Dog.Portal/

RUN dotnet restore Dog.Portal/Dog.Portal.csproj

# Copy all source
COPY Dog.Application/ Dog.Application/
COPY Dog.Portal/ Dog.Portal/

WORKDIR /src/Dog.Portal
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Dog.Portal.dll"]
