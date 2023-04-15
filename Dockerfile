FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app
WORKDIR /app/x64
RUN ln -s /app/data/tessdata/libleptonica-1.80.0.so libleptonica-1.80.0.so
RUN ln -s /app/data/tessdata/libtesseract41.so libtesseract41.so
RUN sed -i'.bak' 's/$/ contrib/' /etc/apt/sources.list
RUN apt-get update
RUN apt-get install -y libc-dev libfontconfig1 fontconfig ttf-bitstream-vera fonts-freefont-ttf ttf-mscorefonts-installer
RUN dpkg -i /src/data/assets/fonts/deb/twemoji.deb
RUN cp /src/data/assets/fonts/ttf/*.ttf /usr/share/fonts/
RUN fc-cache -f
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Sentinel.csproj", "./"]
RUN dotnet restore "Sentinel.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "Sentinel.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sentinel.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sentinel.dll"]
