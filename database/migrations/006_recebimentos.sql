CREATE TABLE IF NOT EXISTS nexus.recebimentos (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    cobranca_id integer NOT NULL REFERENCES nexus.cobrancas (id),
    valor_pago numeric(14, 2) NOT NULL,
    data_pagamento date NOT NULL,
    forma_pagamento varchar(30) NOT NULL,
    CONSTRAINT ck_recebimentos_valor CHECK (valor_pago > 0)
);

CREATE INDEX IF NOT EXISTS ix_recebimentos_cobranca ON nexus.recebimentos (cobranca_id);
CREATE INDEX IF NOT EXISTS ix_recebimentos_data ON nexus.recebimentos (data_pagamento);
