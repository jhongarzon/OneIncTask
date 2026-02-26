FROM nginx:alpine

COPY nginx.conf /etc/nginx/nginx.conf.template
COPY .htpasswd /etc/nginx/.htpasswd

ENV API_UPSTREAM=api:8080
ENV FRONTEND_UPSTREAM=frontend:80
ENV API_HOST=api
ENV FRONTEND_HOST=frontend

CMD ["sh", "-c", "envsubst '$API_UPSTREAM $FRONTEND_UPSTREAM $API_HOST $FRONTEND_HOST' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf && nginx -g 'daemon off;'"]
