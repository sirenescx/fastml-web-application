FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5001
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Fast.ML.WebApp/Fast.ML.WebApp.csproj", "Fast.ML.WebApp/"]
RUN dotnet restore "Fast.ML.WebApp/Fast.ML.WebApp.csproj"
COPY . .
WORKDIR "/src/Fast.ML.WebApp"
RUN dotnet build "Fast.ML.WebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fast.ML.WebApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fast.ML.WebApp.dll"]
