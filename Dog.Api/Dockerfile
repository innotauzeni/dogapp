FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY Dog.Core/Dog.Core.csproj Dog.Core/
COPY Dog.Application/Dog.Application.csproj Dog.Application/
COPY Dog.Infrastructure/Dog.Infrastructure.csproj Dog.Infrastructure/
COPY Dog.Api/Dog.Api.csproj Dog.Api/

RUN dotnet restore Dog.Api/Dog.Api.csproj

# Copy all source
COPY Dog.Core/ Dog.Core/
COPY Dog.Application/ Dog.Application/
COPY Dog.Infrastructure/ Dog.Infrastructure/
COPY Dog.Api/ Dog.Api/

WORKDIR /src/Dog.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Dog.Api.dll"]
