CREATE TABLE IF NOT EXISTS nexus.usuarios (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nome varchar(160) NOT NULL,
    email varchar(180) NOT NULL,
    senha_hash varchar(200) NOT NULL,
    ativo boolean NOT NULL DEFAULT true,
    CONSTRAINT uq_usuarios_email UNIQUE (email)
);
