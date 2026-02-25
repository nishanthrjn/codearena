# Multi-stage build for CodeArena Vite/React frontend
FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies (layer-cached)
COPY package.json ./
RUN npm install

# Build args for environment variables baked into the bundle
ARG VITE_API_URL=http://localhost:5000
ENV VITE_API_URL=$VITE_API_URL

COPY . .
RUN npm run build

# ── Serve with nginx ─────────────────────────────────────────────────────────
FROM nginx:alpine AS final

# Copy built assets
COPY --from=build /app/dist /usr/share/nginx/html

# nginx config: fallback all routes to index.html (SPA routing)
RUN printf 'server {\n\
    listen 80;\n\
    root /usr/share/nginx/html;\n\
    index index.html;\n\
    location / {\n\
    try_files $uri $uri/ /index.html;\n\
    }\n\
    }\n' > /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
