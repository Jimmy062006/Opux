FROM microsoft/dotnet:2.0-sdk

COPY src/Opux /app

WORKDIR /app

RUN dotnet restore \
&& dotnet build

#CMD ["dotnet", "run"]
CMD ["dotnet", "exec", "/app/bin/Debug/netcoreapp2.0/Opux.dll"]
