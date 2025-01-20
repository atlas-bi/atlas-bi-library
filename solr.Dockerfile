# to build
# docker build  --build-arg HOST=host PASSWORD=password USER=user --tag atlas_demo_search -f solr.Dockerfile .

# to run locally
# docker run -i -t -p 8983:8983 -e PORT=8983 -u 0 atlas_demo_search:latest

# to get in shell
# docker run --entrypoint sh -i -t -u 0 atlas_demo_search:latest

# to access webapp
# http://localhost:8983

FROM python:3.13-alpine as search
WORKDIR /app
ARG USER
ARG PASSWORD
ARG HOST
# copy site
COPY ["./web/solr", "./"]

# startup search and load data
RUN apk add --no-cache openjdk11 bash lsof python3-dev curl gcc git py3-pip gcc libc-dev g++ libffi-dev libxml2 unixodbc-dev && \
    pip3 install pyodbc pysolr pytz python-dotenv

# install sql server driver
RUN curl -O https://download.microsoft.com/download/b/9/f/b9f3cce4-3925-46d4-9f46-da08869c6486/msodbcsql18_18.1.1.1-1_amd64.apk && \
    apk add --allow-untrusted msodbcsql18_18.1.1.1-1_amd64.apk

# pull solr etl
RUN mkdir etl && cd etl && git clone --depth 1 https://github.com/atlas-bi/Solr-Search-ETL.git .

# create settings
RUN cd etl && echo "SOLRURL = \"http://localhost:8983/solr/atlas\"" > .env && \
    echo "SOLRLOOKUPURL = \"http://localhost:8983/solr/atlas_lookups\"" >> .env && \
    echo "ATLASDATABASE = \"DRIVER={ODBC Driver 18 for SQL Server};SERVER=$HOST;DATABASE=atlas;UID=$USER;PWD=$PASSWORD;TrustServerCertificate=YES\"" >> .env

# load search
RUN chmod -R 777 bin
RUN bin/solr start -force -noprompt -v && \
 cd etl && \
 python3 -c "import time; time.sleep(30)" && \
 python3 atlas_collections.py && \
 python3 atlas_groups.py && \
 python3 atlas_initiatives.py && \
 python3 atlas_lookups.py && \
 python3 atlas_reports.py && \
 python3 atlas_terms.py && \
 python3 atlas_users.py

FROM alpine:latest
WORKDIR /app
RUN apk add --no-cache openjdk11 bash lsof
COPY --from=search ["/app", "./"]

ARG USER
ARG PASSWORD
ARG HOST

# create config

RUN chmod -R 777 bin
# in release 2022.02.2 we need to change the name from atlas_dotnet to atlas_web
CMD bin/solr start -force -noprompt -f -p $PORT
