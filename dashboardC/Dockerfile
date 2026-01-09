FROM nginx:alpine

RUN apk add --no-cache curl

# Fjern default index så vi er 100% sikre på at vår tar over
RUN rm -f /usr/share/nginx/html/index.html

# Din statiske side
COPY index.html /usr/share/nginx/html/index.html

# Nginx proxy-konfig
COPY default.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD ["curl","-fsS","http://localhost/"]
