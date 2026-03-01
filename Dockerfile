FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "APIseverino.csproj"
RUN dotnet publish "APIseverino.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "APIseverino.dll"]