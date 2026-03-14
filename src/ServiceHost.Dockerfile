ARG SERVICE_PROJECT
ARG SERVICE_DLL

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG SERVICE_PROJECT
ARG SERVICE_DLL
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["NuGet.Config", "./"]
COPY ["Myrati.slnx", "./"]
COPY ["src/Myrati.API/Myrati.API.csproj", "src/Myrati.API/"]
COPY ["src/Myrati.Application/Myrati.Application.csproj", "src/Myrati.Application/"]
COPY ["src/Myrati.BackofficeService.API/Myrati.BackofficeService.API.csproj", "src/Myrati.BackofficeService.API/"]
COPY ["src/Myrati.Domain/Myrati.Domain.csproj", "src/Myrati.Domain/"]
COPY ["src/Myrati.Gateway.API/Myrati.Gateway.API.csproj", "src/Myrati.Gateway.API/"]
COPY ["src/Myrati.IdentityService.API/Myrati.IdentityService.API.csproj", "src/Myrati.IdentityService.API/"]
COPY ["src/Myrati.Infrastructure/Myrati.Infrastructure.csproj", "src/Myrati.Infrastructure/"]
COPY ["src/Myrati.NotificationService.API/Myrati.NotificationService.API.csproj", "src/Myrati.NotificationService.API/"]
COPY ["src/Myrati.PublicService.API/Myrati.PublicService.API.csproj", "src/Myrati.PublicService.API/"]
COPY ["src/Myrati.ServiceDefaults/Myrati.ServiceDefaults.csproj", "src/Myrati.ServiceDefaults/"]
RUN dotnet restore "$SERVICE_PROJECT"

COPY . .
RUN dotnet publish "$SERVICE_PROJECT" -c Release -o /app/publish /p:UseAppHost=false -maxcpucount:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG SERVICE_DLL
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV SERVICE_DLL=${SERVICE_DLL}
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "dotnet $SERVICE_DLL"]
