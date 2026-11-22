FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src
COPY CommentsApp.Domain/*.csproj CommentsApp.Domain/
COPY CommentsApp.Infrastructure/*.csproj CommentsApp.Infrastructure/
COPY CommentsApp.Api/*.csproj CommentsApp.Api/
RUN dotnet restore CommentsApp.Api/CommentsApp.Api.csproj
COPY . .
RUN dotnet publish CommentsApp.Api/CommentsApp.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CommentsApp.Api.dll"]