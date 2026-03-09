FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["NuGet.Config", "./"]
COPY ["Myrati.slnx", "./"]
COPY ["src/Myrati.API/Myrati.API.csproj", "src/Myrati.API/"]
COPY ["src/Myrati.Application/Myrati.Application.csproj", "src/Myrati.Application/"]
COPY ["src/Myrati.Domain/Myrati.Domain.csproj", "src/Myrati.Domain/"]
COPY ["src/Myrati.Infrastructure/Myrati.Infrastructure.csproj", "src/Myrati.Infrastructure/"]
RUN dotnet restore "src/Myrati.API/Myrati.API.csproj"

COPY . .
RUN dotnet publish "src/Myrati.API/Myrati.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Myrati.API.dll"]
