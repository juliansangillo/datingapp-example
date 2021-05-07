# Build angular client
FROM node:15.6-alpine AS client
ARG environment
WORKDIR /source
COPY client client/
RUN npm install -g @angular/cli
WORKDIR /source/client
RUN npm install
RUN ng build --configuration=$environment

# Build dotnet app
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /source
COPY API API/
COPY --from=client /source/API/wwwroot API/wwwroot
WORKDIR /source/API
RUN dotnet publish -c Release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "API.dll"]