# --- Estágio 1: Compilação (Build) ---
# Usamos a imagem do .NET 6 SDK para compilar o projeto.
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build 
WORKDIR /src

# Copia os arquivos de projeto primeiro para aproveitar o cache do Docker.
COPY *.csproj ./
RUN dotnet restore

# Copia o resto do código-fonte.
COPY . .

# Publica a aplicação em modo Release.
RUN dotnet publish -c Release -o /app/publish --no-restore

# --- Estágio 2: Execução (Runtime) ---
# Usamos a imagem runtime do .NET 6, que é muito menor.
FROM mcr.microsoft.com/dotnet/runtime:6.0 AS final 
WORKDIR /app

# Copia apenas os arquivos publicados do estágio de build.
COPY --from=build /app/publish .

# Define o comando que será executado quando o container iniciar.
# O nome do .dll é o mesmo do seu projeto/arquivo .csproj.
ENTRYPOINT ["dotnet", "eletron-datalog-rabbit-consumer.dll"]