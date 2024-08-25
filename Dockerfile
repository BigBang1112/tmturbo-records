FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH=x64
ARG APPNAME=TMTurboRecords
RUN apt-get update && apt-get install -y git python3
WORKDIR /src

COPY .git/ ./.git/

RUN dotnet workload install wasm-tools

# copy csproj and restore as distinct layers
COPY $APPNAME/$APPNAME/*.csproj $APPNAME/
COPY $APPNAME/$APPNAME.Client/*.csproj $APPNAME.Client/
COPY $APPNAME/$APPNAME.Shared/*.csproj $APPNAME.Shared/
RUN dotnet restore $APPNAME/$APPNAME.csproj -a $TARGETARCH

# copy and publish app and libraries
COPY $APPNAME/ .
RUN dotnet publish $APPNAME -c $BUILD_CONFIGURATION -a $TARGETARCH -o /app --no-restore


# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled
EXPOSE 8080
EXPOSE 8081
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["./TMTurboRecords"]
