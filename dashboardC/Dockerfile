FROM nginx:alpine

# Nginx-konfig
COPY default.conf /etc/nginx/conf.d/default.conf

# Kopier ALT web-innhold til dokumentroten
# (pga .dockerignore blir ikke Dockerfile/default.conf med)
COPY . /usr/share/nginx/html/

EXPOSE 80
