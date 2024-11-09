FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Переключаемся на root для установки ffmpeg
USER root

# Установка ffmpeg
RUN apt-get update -y && \
    apt-get install -y ffmpeg && \
    rm -rf /var/lib/apt/lists/*
    
# Возвращаемся к пользователю приложения
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"
COPY ["DelphicGames/DelphicGames.csproj", "DelphicGames/"]
RUN dotnet restore "DelphicGames/DelphicGames.csproj"
COPY . .
WORKDIR "/src/DelphicGames"
RUN dotnet build "DelphicGames.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DelphicGames.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN ffmpeg -version
ENTRYPOINT ["dotnet", "DelphicGames.dll"]
