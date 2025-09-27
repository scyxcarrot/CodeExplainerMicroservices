FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["common_appsettings.Development.json", "./"]
COPY ["common_appsettings.json", "./"]
COPY ["UserService/UserService.csproj", "UserService/"]
COPY ["CodeExplainerCommon/CodeExplainerCommon.csproj", "CodeExplainerCommon/"]
RUN dotnet restore "./UserService/UserService.csproj"
COPY . .
WORKDIR "/src/UserService"
RUN dotnet build "./UserService.csproj" -c Release -o /output

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
COPY ["common_appsettings.Development.json", "./"]
COPY ["common_appsettings.json", "./"]
WORKDIR /app
COPY --from=build /output .

# for debugging purposes
ENV ASPNETCORE_HTTP_PORTS=7093

ENTRYPOINT ["dotnet", "UserService.dll"]