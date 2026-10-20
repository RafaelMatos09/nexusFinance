CREATE TABLE IF NOT EXISTS nexus.contratos (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    cliente_id integer NOT NULL REFERENCES nexus.clientes (id),
    descricao varchar(300) NOT NULL,
    valor numeric(14, 2) NOT NULL,
    periodicidade varchar(20) NOT NULL,
    data_inicio date NOT NULL,
    data_fim date,
    status varchar(20) NOT NULL,
    CONSTRAINT ck_contratos_valor CHECK (valor > 0),
    CONSTRAINT ck_contratos_periodo CHECK (data_fim IS NULL OR data_fim >= data_inicio)
);

CREATE INDEX IF NOT EXISTS ix_contratos_cliente ON nexus.contratos (cliente_id);
CREATE INDEX IF NOT EXISTS ix_contratos_status ON nexus.contratos (status);
