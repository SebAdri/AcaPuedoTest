FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ./src ./src
RUN dotnet restore ./src/Payments.Api/Payments.Api.csproj
RUN dotnet publish ./src/Payments.Api/Payments.Api.csproj -c Release -o /app
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet","Payments.Api.dll"]
