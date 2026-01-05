FROM nginx:alpine

# Verktøy for healthcheck
RUN apk add --no-cache curl

# Statisk frontend
COPY index.html /usr/share/nginx/html/index.html

# Nginx som proxy for /mqtt -> mosquitto:9001
COPY default.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

# Healthcheck: OK hvis nginx svarer 200
HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD ["curl","-fsS","http://localhost/"]

