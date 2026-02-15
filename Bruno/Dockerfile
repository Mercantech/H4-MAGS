# Bruno CLI – kør E2E-UserFlows mod en baseUrl (sættes i docker-compose)
FROM node:20-alpine

RUN npm install -g @usebruno/cli

WORKDIR /collection

ENTRYPOINT ["bru"]
CMD ["run", "--env", "Kahoot", "E2E-UserFlows"]
