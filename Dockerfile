# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and projects
COPY ["LogoVisualizer.sln", "./"]
COPY ["LogoVisualizer.Api/LogoVisualizer.Api.csproj", "LogoVisualizer.Api/"]
COPY ["LogoVisualizer.Data/LogoVisualizer.Data.csproj", "LogoVisualizer.Data/"]

# Restore dependencies
RUN dotnet restore "LogoVisualizer.sln"

# Copy all source files
COPY . .

# Build
RUN dotnet build "LogoVisualizer.sln" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "LogoVisualizer.Api/LogoVisualizer.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "LogoVisualizer.Api.dll"]
