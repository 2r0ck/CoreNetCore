FROM rnyarulin/netcoresdk
COPY . .
RUN dotnet restore -s https://api.nuget.org/v3/index.json -s http://haproxy.lb.dbs.grp.gloria-jeans.ru:1080/v3/index.json 
RUN dotnet build -c release CoreNetCore
 