-- Nexus Finance
-- O banco neondb já existe no Neon. Esta migration só prepara o controle de scripts.
-- Não executa CREATE DATABASE.

CREATE TABLE IF NOT EXISTS schema_migrations (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome varchar(200) NOT NULL UNIQUE,
    aplicada_em timestamptz NOT NULL DEFAULT now()
);

INSERT INTO schema_migrations (nome)
VALUES ('001_init.sql')
ON CONFLICT (nome) DO NOTHING;
