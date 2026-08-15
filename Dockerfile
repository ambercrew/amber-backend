FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV HUSKY=0
WORKDIR /src

COPY Amber.Domain/Amber.Domain.csproj Amber.Domain/
COPY Amber.Application/Amber.Application.csproj Amber.Application/
COPY Amber.Infrastructure/Amber.Infrastructure.csproj Amber.Infrastructure/
COPY Amber.WebApi/Amber.WebApi.csproj Amber.WebApi/

RUN dotnet restore Amber.WebApi/Amber.WebApi.csproj

COPY Amber.Domain/ Amber.Domain/
COPY Amber.Application/ Amber.Application/
COPY Amber.Infrastructure/ Amber.Infrastructure/
COPY Amber.WebApi/ Amber.WebApi/

RUN dotnet publish Amber.WebApi/Amber.WebApi.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Without it the following error is produced:
# Error: libgssapi_krb5.so.2: cannot open shared object file: No such file or directory

RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2

WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Amber.WebApi.dll"]
