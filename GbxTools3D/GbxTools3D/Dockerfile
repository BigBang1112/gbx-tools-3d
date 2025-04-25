# Debug phase
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Publish phase
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS publish
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
ARG APPNAME=GbxTools3D
RUN apt-get update && apt-get install -y git python3
WORKDIR /src
RUN dotnet workload install wasm-tools

# copy csproj and restore as distinct layers
COPY nuget.config ./
COPY $APPNAME/$APPNAME/*.csproj $APPNAME/$APPNAME/
COPY $APPNAME/$APPNAME.Client/*.csproj $APPNAME/$APPNAME.Client/
RUN dotnet restore $APPNAME/$APPNAME/$APPNAME.csproj -a $TARGETARCH

# copy and publish app and libraries
COPY $APPNAME/$APPNAME $APPNAME/$APPNAME
COPY $APPNAME/$APPNAME.Client $APPNAME/$APPNAME.Client
RUN dotnet publish $APPNAME/$APPNAME -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app /p:UseAppHost=false --no-restore


# Final phase
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS final
EXPOSE 8080
EXPOSE 8081
WORKDIR /app
COPY --from=publish /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "GbxTools3D.dll"]