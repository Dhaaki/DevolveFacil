FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Src/DevolveFacill.Api/DevolveFacill.Api.csproj", "Src/DevolveFacill.Api/"]
COPY ["Src/DevolveFacill.Core/DevolveFacill.Core.csproj", "Src/DevolveFacill.Core/"]
COPY ["Src/DevolveFacill.Infrastructure/DevolveFacill.Infrastructure.csproj", "Src/DevolveFacill.Infrastructure/"]
COPY ["Src/DevolveFacill.Adapters/DevolveFacill.Adapters.csproj", "Src/DevolveFacill.Adapters/"]
COPY ["Src/DevolveFacill.Workers/DevolveFacill.Workers.csproj", "Src/DevolveFacill.Workers/"]

RUN dotnet restore "Src/DevolveFacill.Api/DevolveFacill.Api.csproj"

COPY . .
WORKDIR "/src/Src/DevolveFacill.Api"
RUN dotnet build "DevolveFacill.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DevolveFacill.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DevolveFacill.Api.dll"]
