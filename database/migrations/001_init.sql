-- Nexus Finance
-- O banco neondb já existe no Neon. O schema nexus isola estas tabelas do restante do banco.

CREATE SCHEMA IF NOT EXISTS nexus;

CREATE TABLE IF NOT EXISTS nexus.schema_migrations (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome varchar(200) NOT NULL UNIQUE,
    aplicada_em timestamptz NOT NULL DEFAULT now()
);

INSERT INTO nexus.schema_migrations (nome)
VALUES ('001_init.sql')
ON CONFLICT (nome) DO NOTHING;
