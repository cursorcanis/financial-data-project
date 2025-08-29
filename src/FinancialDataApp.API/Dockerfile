# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY FinancialDataApp.sln ./
COPY src/FinancialDataApp.Core/FinancialDataApp.Core.csproj src/FinancialDataApp.Core/
COPY src/FinancialDataApp.Infrastructure/FinancialDataApp.Infrastructure.csproj src/FinancialDataApp.Infrastructure/
COPY src/FinancialDataApp.API/FinancialDataApp.API.csproj src/FinancialDataApp.API/
RUN dotnet restore src/FinancialDataApp.API/FinancialDataApp.API.csproj

# Copy the rest of the source and publish
COPY src/ ./src/
RUN dotnet publish src/FinancialDataApp.API/FinancialDataApp.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FinancialDataApp.API.dll"]
