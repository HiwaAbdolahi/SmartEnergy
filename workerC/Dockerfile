# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# bedre cache
COPY SmartEnergy.csproj ./
RUN dotnet restore

# bygg
COPY . .
RUN dotnet build SmartEnergy.csproj -c Release -o /app/build

# publish
FROM build AS publish
RUN dotnet publish SmartEnergy.csproj -c Release -o /app/publish /p:UseAppHost=false

# runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# enkel healthcheck (OK hvis .NET kjører i containeren)
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
  CMD ["sh","-c","pgrep -f SmartEnergy >/dev/null || exit 1"]


ENTRYPOINT ["dotnet", "SmartEnergy.dll"]
