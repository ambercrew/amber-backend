FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV HUSKY=0
WORKDIR /src

COPY Brainy.Domain/Brainy.Domain.csproj Brainy.Domain/
COPY Brainy.Application/Brainy.Application.csproj Brainy.Application/
COPY Brainy.Infrastructure/Brainy.Infrastructure.csproj Brainy.Infrastructure/
COPY Brainy.WebApi/Brainy.WebApi.csproj Brainy.WebApi/

RUN dotnet restore Brainy.WebApi/Brainy.WebApi.csproj

COPY Brainy.Domain/ Brainy.Domain/
COPY Brainy.Application/ Brainy.Application/
COPY Brainy.Infrastructure/ Brainy.Infrastructure/
COPY Brainy.WebApi/ Brainy.WebApi/

RUN dotnet publish Brainy.WebApi/Brainy.WebApi.csproj \
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

ENTRYPOINT ["dotnet", "Brainy.WebApi.dll"]
