FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ChatService/ChatService.csproj", "ChatService/"]
COPY ["CodeExplainerCommon/CodeExplainerCommon.csproj", "CodeExplainerCommon/"]
RUN dotnet restore "./ChatService/ChatService.csproj"
COPY . .
WORKDIR "/src/ChatService"
RUN dotnet build "./ChatService.csproj" -c Release -o /output

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
COPY ["common_appsettings.Development.json", "./"]
COPY ["common_appsettings.json", "./"]
WORKDIR /app
COPY --from=build /output .

# for debugging purposes
ENV ASPNETCORE_HTTP_PORTS=7272

ENTRYPOINT ["dotnet", "ChatService.dll"]