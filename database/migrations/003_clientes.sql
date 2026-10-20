CREATE TABLE IF NOT EXISTS nexus.clientes (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome varchar(200) NOT NULL,
    documento varchar(14) NOT NULL,
    email varchar(180) NOT NULL,
    telefone varchar(20),
    status varchar(20) NOT NULL,
    criado_em timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_clientes_documento UNIQUE (documento)
);

CREATE INDEX IF NOT EXISTS ix_clientes_nome ON nexus.clientes (nome);
CREATE INDEX IF NOT EXISTS ix_clientes_status ON nexus.clientes (status);
