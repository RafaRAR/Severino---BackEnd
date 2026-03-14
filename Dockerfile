FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

RUN apt-get update && apt-get install -y locales \
    && sed -i -e 's/# pt_BR.UTF-8 UTF-8/pt_BR.UTF-8 UTF-8/' /etc/locale.gen \
    && locale-gen pt_BR.UTF-8

ENV LANG=pt_BR.UTF-8
ENV LC_ALL=pt_BR.UTF-8
ENV LANG=C.UTF-8
ENV LC_ALL=C.UTF-8
ENV LANGUAGE=pt_BR:pt

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY APIseverino.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish APIseverino.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "APIseverino.dll"]