run:
	cd src/ && dotnet run --project RailFactory.AppHost

run-hot-reload:
	cd src/ && dotnet watch run --project RailFactory.AppHost

run-clean:
	dotnet clean src/RailFactory.AppHost
	dotnet clean src/RailFactory.Gateway

# Frontend is the single public entry (port 5082). It talks to microservices via the Gateway.
run-ngrok:
	ngrok http --domain=apparent-driving-horse.ngrok-free.app 5082

run-diagrams:
	./scripts/render-plantuml-diagrams.sh