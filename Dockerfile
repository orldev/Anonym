﻿FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Server/Server.csproj", "Server/"]
COPY ["Client/Client.csproj", "Client/"]
RUN dotnet restore "Server/Server.csproj"
COPY . .
WORKDIR "/src/Server"
RUN dotnet build "Server.csproj" -c $BUILD_CONFIGURATION -o /app/build --runtime linux-x64

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --runtime linux-x64

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Server.dll"]
