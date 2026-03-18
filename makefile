run:
	cd src/ && dotnet run --project RailFactory.AppHost

run-hot-reload:
	cd src/ && dotnet watch run --project RailFactory.AppHost

run-clean:
	dotnet clean src/RailFactory.AppHost
	dotnet clean src/RailFactory.Gateway

run-ngrok:
	ngrok http --domain=apparent-driving-horse.ngrok-free.app 5080

run-diagrams:
	./scripts/render-plantuml-diagrams.sh