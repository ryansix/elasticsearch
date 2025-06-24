# ELK with APM-Server
## Start Elasticsearch and Kibana
- Navigate to elk
- Exec `docker-compose up -d`
- Healthcheck for below endpoint
  - Kibana: http://<IP>:5061
    UserName/Password: elastic/password
  - Elasticsearch: https://<IP>:9200
    UserName/Password: elastic/password
## Start APM-Server
- Navigate to apm-server
- Exec `docker-compose up -d`
- Healthcheck for below endpoint
  - apm-server: http://<IP>:8200
