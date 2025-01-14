# to build
# docker build  --tag atlas_demo . --build-arg HOST=host PASSWORD=password USER=user

# to run locally
# docker run -i -t -p 1234:1234 -p 8983:8983 -e PORT=1234  -u 0 atlas_demo:latest

# to get in shell
# docker run --entrypoint sh -i -t -u 0 atlas_demo:latest

# to access webapp
# http://localhost:1234

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

WORKDIR /app

COPY web.sln .
COPY ["./web/web.csproj", "./web/"]
RUN dotnet restore ./web/web.csproj

COPY ["./web/.", "./web/"]

WORKDIR "/app/web/"

# add analytics
RUN  sed -i -e 's/<\/body>/<script async defer data-website-id="fb4377bf-3d8a-40f7-97f9-c8e57e11c953" src="https:\/\/analytics.atlas.bi\/script.js"><\/script><\/body>/g' Pages/Shared/_Layout.cshtml

ARG USER \
    PASSWORD \
    HOST \
    SOLR

# create config
RUN echo "{\"Demo\": true, \"solr\": {\"atlas_address\": \"$SOLR/solr/atlas\", \"atlas_lookups_address\": \"$SOLR/solr/atlas_lookups\"},\"ConnectionStrings\": {\"AtlasDatabase\": \"Server=$HOST;Database=atlas;User Id=$USER; Password=$PASSWORD; MultipleActiveResultSets=true;TrustServerCertificate=YES\"},  \"footer\": {\"links\":{\"Status\": {\"Status\": \"https://status.atlas.bi/status/atlas\", \"Documentation\": \"https://atlas.bi\", \"Source Code\": \"https://github.com/atlas-bi/atlas-bi-library\" }},\"subtitle\": \"Atlas was created by the Riverside Healthcare Analytics team.\"}}" > appsettings.cust.json

# migrate
RUN dotnet tool install --global dotnet-ef \
  && export PATH="$PATH:/root/.dotnet/tools" \
  && dotnet tool restore

RUN  export PATH="$PATH:/root/.dotnet/tools" && dotnet ef database update --project web.csproj -v

RUN dotnet publish -c Release -o out web.csproj

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
WORKDIR /app
COPY --from=build ["/app/web/out", "./"]

CMD ASPNETCORE_URLS=http://*:$PORT dotnet "Atlas_Web.dll"
