FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Shared/FluxoCaixaDiario.SharedKernel/FluxoCaixaDiario.SharedKernel.csproj", "src/Shared/FluxoCaixaDiario.SharedKernel/"]
COPY ["src/Consolidado/FluxoCaixaDiario.Consolidado.Domain/FluxoCaixaDiario.Consolidado.Domain.csproj", "src/Consolidado/FluxoCaixaDiario.Consolidado.Domain/"]
COPY ["src/Consolidado/FluxoCaixaDiario.Consolidado.Application/FluxoCaixaDiario.Consolidado.Application.csproj", "src/Consolidado/FluxoCaixaDiario.Consolidado.Application/"]
COPY ["src/Consolidado/FluxoCaixaDiario.Consolidado.Infrastructure/FluxoCaixaDiario.Consolidado.Infrastructure.csproj", "src/Consolidado/FluxoCaixaDiario.Consolidado.Infrastructure/"]
COPY ["src/Consolidado/FluxoCaixaDiario.Consolidado.API/FluxoCaixaDiario.Consolidado.API.csproj", "src/Consolidado/FluxoCaixaDiario.Consolidado.API/"]
RUN dotnet restore "src/Consolidado/FluxoCaixaDiario.Consolidado.API/FluxoCaixaDiario.Consolidado.API.csproj"
COPY . .
WORKDIR "/src/src/Consolidado/FluxoCaixaDiario.Consolidado.API"
RUN dotnet build "FluxoCaixaDiario.Consolidado.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FluxoCaixaDiario.Consolidado.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FluxoCaixaDiario.Consolidado.API.dll"]