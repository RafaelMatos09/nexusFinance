CREATE TABLE IF NOT EXISTS nexus.configuracoes (
    chave varchar(80) PRIMARY KEY,
    valor numeric(10, 4) NOT NULL
);

INSERT INTO nexus.configuracoes (chave, valor)
VALUES ('multa_percentual', 2), ('juros_mensal_percentual', 1)
ON CONFLICT (chave) DO NOTHING;
