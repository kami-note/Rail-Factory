# Rail Factory — run everything (gateway + backends)
# Usage: make run-all   → starts Docker and all services in background
#        make stop      → stops all backends and optionally Docker
# See docs/10_Infrastructure_And_Env.md for ports and gateway routes.

SHELL := /bin/bash
ROOT := $(CURDIR)
PIDFILE := $(ROOT)/.rail-factory-pids

# Backend projects and ports (must match nginx/nginx.conf and doc 10)
IAM         := src/RailFactory.IAM
PRODUCTION  := src/RailFactory.Production
DASHBOARD   := src/RailFactory.Dashboard
SUPPLYCHAIN := src/RailFactory.SupplyChain
LOGISTICS   := src/RailFactory.Logistics
FLEET       := src/RailFactory.Fleet
HCM         := src/RailFactory.HCM

PORTS := 5243 5244 5245 5246 5247 5248 5249
PROJECTS := $(IAM) $(PRODUCTION) $(DASHBOARD) $(SUPPLYCHAIN) $(LOGISTICS) $(FLEET) $(HCM)

.PHONY: help up down build run-all stop free-ports test-routes run-iam run-production run-dashboard run-supplychain run-logistics run-fleet run-hcm status

help:
	@echo "Rail Factory — targets"
	@echo "  make up          — start Docker (gateway, postgres, redis, rabbitmq)"
	@echo "  make down        — stop Docker"
	@echo "  make build       — build solution"
	@echo "  make run-all     — stop any old backends, free ports, then start Docker + all 7 backends (5243–5249)"
	@echo "  make stop        — stop all backend processes started by run-all (uses PID file)"
	@echo "  make free-ports  — kill any process listening on 5243–5249 (use if stop did not free ports)"
	@echo "  make test-routes — test gateway and /api/* health endpoints (run after run-all)"
	@echo "  make run-iam ... — run single service (foreground)"
	@echo "  make status      — show Docker and backend PIDs"

up:
	docker compose up -d

down:
	docker compose down

build:
	dotnet build RailFactory.sln

# Free backend ports (5243–5249); use lsof if available, else fuser
free-ports:
	@for port in $(PORTS); do \
		pid=$$(lsof -ti:$$port 2>/dev/null); \
		if [ -n "$$pid" ]; then echo "Killing process on port $$port (PID $$pid)"; kill $$pid 2>/dev/null || true; fi; \
	done
	@rm -f "$(PIDFILE)"

# Start all backends in background and save PIDs (stops old backends and frees ports first)
# Single shell block so each PID is written once (no duplicates)
run-all: stop free-ports build up
	@echo "Waiting for gateway and DB..."
	@sleep 3
	@rm -f "$(PIDFILE)"; \
	echo "Starting backends..."; \
	cd "$(ROOT)" && dotnet run --project $(IAM)         --urls http://0.0.0.0:5243 --no-build & echo $$! >> "$(PIDFILE)"; sleep 1; \
	cd "$(ROOT)" && dotnet run --project $(PRODUCTION)  --urls http://0.0.0.0:5244 --no-build & echo $$! >> "$(PIDFILE)"; sleep 1; \
	cd "$(ROOT)" && dotnet run --project $(DASHBOARD)   --urls http://0.0.0.0:5245 --no-build & echo $$! >> "$(PIDFILE)"; sleep 1; \
	cd "$(ROOT)" && dotnet run --project $(SUPPLYCHAIN) --urls http://0.0.0.0:5246 --no-build & echo $$! >> "$(PIDFILE)"; sleep 1; \
	cd "$(ROOT)" && dotnet run --project $(LOGISTICS)   --urls http://0.0.0.0:5247 --no-build & echo $$! >> "$(PIDFILE)"; sleep 1; \
	cd "$(ROOT)" && dotnet run --project $(FLEET)       --urls http://0.0.0.0:5248 --no-build & echo $$! >> "$(PIDFILE)"; sleep 1; \
	cd "$(ROOT)" && dotnet run --project $(HCM)         --urls http://0.0.0.0:5249 --no-build & echo $$! >> "$(PIDFILE)"; \
	echo "Backends started. PIDs in $(PIDFILE)"; \
	echo "Gateway: http://localhost (or http://localhost:80)"; \
	echo "  e.g. http://localhost/api/iam/health  http://localhost/gateway/health"

# Test routes via gateway (run on host after run-all; requires curl)
test-routes:
	@echo "Testing routes (http://localhost)..."
	@ok=0; fail=0; \
	check() { code=$$(curl -s -o /dev/null -w "%{http_code}" "$$1" ${2:-}); \
	  if [ "$$code" = "200" ]; then echo "  OK  $$code $$1"; ok=$$((ok+1)); else echo "  FAIL $$code $$1"; fail=$$((fail+1)); fi; }; \
	check "http://localhost/"; \
	check "http://localhost/gateway/health"; \
	check "http://localhost/api/iam/health"; \
	check "http://localhost/api/iam/tenant" '-H "X-Tenant-Id: default"'; \
	check "http://localhost/api/production/health"; \
	check "http://localhost/api/dashboard/health"; \
	check "http://localhost/api/supplychain/health"; \
	check "http://localhost/api/logistics/health"; \
	check "http://localhost/api/fleet/health"; \
	check "http://localhost/api/hcm/health"; \
	echo ""; echo "Result: $$ok passed, $$fail failed"

stop:
	@if [ -f "$(PIDFILE)" ]; then \
		echo "Stopping backends..."; \
		sort -u "$(PIDFILE)" | while read pid; do kill $$pid 2>/dev/null || true; done; \
		rm -f "$(PIDFILE)"; \
		echo "Backends stopped."; \
	else \
		echo "No PID file found (run-all was not used or already stopped)."; \
	fi

status:
	@echo "Docker:"
	@docker compose ps 2>/dev/null || true
	@echo ""
	@if [ -f "$(PIDFILE)" ]; then echo "Backend PIDs:"; sort -u "$(PIDFILE)"; else echo "No backend PIDs file."; fi

# Single-service targets (foreground; use in separate terminals or for debugging)
run-iam:
	dotnet run --project $(IAM) --urls http://localhost:5243

run-production:
	dotnet run --project $(PRODUCTION) --urls http://localhost:5244

run-dashboard:
	dotnet run --project $(DASHBOARD) --urls http://localhost:5245

run-supplychain:
	dotnet run --project $(SUPPLYCHAIN) --urls http://localhost:5246

run-logistics:
	dotnet run --project $(LOGISTICS) --urls http://localhost:5247

run-fleet:
	dotnet run --project $(FLEET) --urls http://localhost:5248

run-hcm:
	dotnet run --project $(HCM) --urls http://localhost:5249
