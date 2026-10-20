CREATE TABLE IF NOT EXISTS nexus.cobrancas (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    contrato_id integer NOT NULL REFERENCES nexus.contratos (id),
    cliente_id integer NOT NULL REFERENCES nexus.clientes (id),
    valor numeric(14, 2) NOT NULL,
    vencimento date NOT NULL,
    status varchar(20) NOT NULL,
    observacao varchar(500),
    CONSTRAINT uq_cobrancas_contrato_vencimento UNIQUE (contrato_id, vencimento),
    CONSTRAINT ck_cobrancas_valor CHECK (valor > 0)
);

CREATE INDEX IF NOT EXISTS ix_cobrancas_cliente ON nexus.cobrancas (cliente_id);
CREATE INDEX IF NOT EXISTS ix_cobrancas_status ON nexus.cobrancas (status);
CREATE INDEX IF NOT EXISTS ix_cobrancas_vencimento ON nexus.cobrancas (vencimento);
