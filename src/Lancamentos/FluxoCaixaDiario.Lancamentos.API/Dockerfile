FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Shared/FluxoCaixaDiario.SharedKernel/FluxoCaixaDiario.SharedKernel.csproj", "src/Shared/FluxoCaixaDiario.SharedKernel/"]
COPY ["src/Lancamentos/FluxoCaixaDiario.Lancamentos.Domain/FluxoCaixaDiario.Lancamentos.Domain.csproj", "src/Lancamentos/FluxoCaixaDiario.Lancamentos.Domain/"]
COPY ["src/Lancamentos/FluxoCaixaDiario.Lancamentos.Application/FluxoCaixaDiario.Lancamentos.Application.csproj", "src/Lancamentos/FluxoCaixaDiario.Lancamentos.Application/"]
COPY ["src/Lancamentos/FluxoCaixaDiario.Lancamentos.Infrastructure/FluxoCaixaDiario.Lancamentos.Infrastructure.csproj", "src/Lancamentos/FluxoCaixaDiario.Lancamentos.Infrastructure/"]
COPY ["src/Lancamentos/FluxoCaixaDiario.Lancamentos.API/FluxoCaixaDiario.Lancamentos.API.csproj", "src/Lancamentos/FluxoCaixaDiario.Lancamentos.API/"]
RUN dotnet restore "src/Lancamentos/FluxoCaixaDiario.Lancamentos.API/FluxoCaixaDiario.Lancamentos.API.csproj"
COPY . .
WORKDIR "/src/src/Lancamentos/FluxoCaixaDiario.Lancamentos.API"
RUN dotnet build "FluxoCaixaDiario.Lancamentos.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FluxoCaixaDiario.Lancamentos.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FluxoCaixaDiario.Lancamentos.API.dll"]