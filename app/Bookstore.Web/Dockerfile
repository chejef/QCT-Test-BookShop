FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS base
WORKDIR /app
EXPOSE 8080

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["app/Bookstore.Common/Bookstore.Common.csproj", "app/Bookstore.Common/"]
COPY ["app/Bookstore.Data/Bookstore.Data.csproj", "app/Bookstore.Data/"]
COPY ["app/Bookstore.Domain/Bookstore.Domain.csproj", "app/Bookstore.Domain/"]
COPY ["app/Bookstore.Web/Bookstore.Web.csproj", "app/Bookstore.Web/"]
RUN dotnet restore "app/Bookstore.Web/Bookstore.Web.csproj" -a amd64
COPY . .
WORKDIR "/src/app/Bookstore.Web"
RUN dotnet build "Bookstore.Web.csproj" -c Release -o /app/build -a amd64

FROM build AS publish
RUN dotnet publish "Bookstore.Web.csproj" -c Release -o /app/publish -a amd64 --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bookstore.Web.dll"]
