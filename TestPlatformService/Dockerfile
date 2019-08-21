FROM    mcr.microsoft.com/dotnet/core/aspnet:2.2.6-alpine3.9
COPY  ./publish .
EXPOSE 8048/tcp
ENTRYPOINT ["dotnet", "TestPlatformService.dll"]
