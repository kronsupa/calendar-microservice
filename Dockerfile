# Stage 1 Build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /SRC

#restore
COPY ["/SRC/calendar-microservice.csproj", "calendar-microservice/"]
RUN dotnet restore 'calendar-microservice/calendar-microservice.csproj'

# build
COPY ["/SRC/", "calendar-microservice/"]
RUN dotnet build 'calendar-microservice/calendar-microservice.csproj' -c Release -o app/build

# Stage 2 Publish

FROM build AS publish
RUN dotnet publish 'calendar-microservice/calendar-microservice.csproj' -c Release -o /app/publish

# Stage 3 Run
FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 8080
WORKDIR /app/publish
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "calendar-microservice.dll"]