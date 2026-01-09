FROM nginx:alpine

RUN apk add --no-cache curl ca-certificates && \
    rm -f /usr/share/nginx/html/index.html

COPY index.html /usr/share/nginx/html/index.html
COPY default.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD ["curl","-fsS","http://localhost/"]
