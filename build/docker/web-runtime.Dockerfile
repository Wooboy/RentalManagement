FROM nginx:1.27-alpine

COPY web/dist/ /usr/share/nginx/html/
RUN chown -R nginx:nginx /usr/share/nginx/html \
  && find /usr/share/nginx/html -type d -exec chmod 755 {} \; \
  && find /usr/share/nginx/html -type f -exec chmod 644 {} \;
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
