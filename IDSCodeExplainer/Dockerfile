FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["IDSCodeExplainer/IDSCodeExplainer.csproj", "IDSCodeExplainer/"]
COPY ["CodeExplainerCommon/CodeExplainerCommon.csproj", "CodeExplainerCommon/"]
RUN dotnet restore "./IDSCodeExplainer/IDSCodeExplainer.csproj"
COPY . .
WORKDIR "/src/IDSCodeExplainer"
RUN dotnet build "./IDSCodeExplainer.csproj" -c Release -o /output

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
COPY ["common_appsettings.Development.json", "./"]
COPY ["common_appsettings.json", "./"]
WORKDIR /app
COPY --from=build /output .

# for debugging purposes
ENV ASPNETCORE_HTTP_PORTS=7043

ENTRYPOINT ["dotnet", "IDSCodeExplainer.dll"]