# Use official Node.js image as the base
FROM node:22.14.0-alpine

ARG NEXT_PUBLIC_API_BASE_URL
ARG API_PROXY_TARGET
ENV NEXT_PUBLIC_API_BASE_URL=${NEXT_PUBLIC_API_BASE_URL}
ENV API_PROXY_TARGET=${API_PROXY_TARGET}

# Set working directory
WORKDIR /app

# Copy package.json and package-lock.json
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy the rest of the application code
COPY . .

# Build the Next.js app
RUN npm run build

# Expose port 3000
EXPOSE 3000

# Start the Next.js server
CMD ["npm", "start"]