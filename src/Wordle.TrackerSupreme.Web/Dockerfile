FROM node:20-alpine AS build
WORKDIR /app

# Add build args
ARG COMMIT_SHA=dev
ARG REPO_URL=https://github.com/HansBakN/wordle.trackersupreme

# Expose to SvelteKit (must be PUBLIC_)
ENV PUBLIC_COMMIT_SHA=$COMMIT_SHA
ENV PUBLIC_COMMIT_URL=$REPO_URL/commit/$COMMIT_SHA

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM node:20-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV HOST=0.0.0.0
ENV PORT=3000

COPY package*.json ./
RUN npm ci --omit=dev
COPY --from=build /app/build ./build

EXPOSE 3000
CMD ["node", "build"]
