#!/bin/bash
# Create IAM database on same PostgreSQL instance (Phase 1). Run by docker-entrypoint-initdb.d.
set -e
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  SELECT 'CREATE DATABASE railfactory_iam' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'railfactory_iam')\gexec
EOSQL
