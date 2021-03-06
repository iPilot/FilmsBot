#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["FilmsBot.csproj", "."]
RUN dotnet restore "./FilmsBot.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "FilmsBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FilmsBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV LANG en_US.UTF-8  
ENV LANGUAGE en_US:en  
ENV LC_ALL en_US.UTF-8   

ENTRYPOINT ["dotnet", "FilmsBot.dll"]