-- =====================================================
-- Script de Inicialização do Banco de Dados
-- IoT Sensor Monitoring System
-- =====================================================

-- Configurações
SET client_encoding = 'UTF8';
SET timezone = 'UTC';

-- =====================================================
-- TABELA: Setores
-- =====================================================
CREATE TABLE IF NOT EXISTS "Setores" (
    "Id" SERIAL PRIMARY KEY,
    "Nome" VARCHAR(200) NOT NULL,
    "Descricao" VARCHAR(500),
    "EstaAtivo" BOOLEAN NOT NULL DEFAULT TRUE
);

-- =====================================================
-- TABELA: Sensores
-- =====================================================
CREATE TABLE IF NOT EXISTS "Sensores" (
    "Id" INTEGER PRIMARY KEY,
    "Codigo" VARCHAR(50) NOT NULL,
    "Nome" VARCHAR(200) NOT NULL,
    "Descricao" VARCHAR(500),
    "EstaAtivo" BOOLEAN NOT NULL DEFAULT TRUE,
    "CriadoEm" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Sensores_Codigo" ON "Sensores" ("Codigo");

-- =====================================================
-- TABELA: Equipamentos
-- =====================================================
CREATE TABLE IF NOT EXISTS "Equipamentos" (
    "Id" SERIAL PRIMARY KEY,
    "Nome" VARCHAR(200) NOT NULL,
    "Descricao" VARCHAR(500),
    "IdSetor" INTEGER NOT NULL,
    "EstaAtivo" BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT "FK_Equipamentos_Setores" FOREIGN KEY ("IdSetor")
        REFERENCES "Setores" ("Id") ON DELETE RESTRICT
);

-- =====================================================
-- TABELA: Medicoes
-- =====================================================
CREATE TABLE IF NOT EXISTS "Medicoes" (
    "Id" BIGSERIAL PRIMARY KEY,
    "IdSensor" INTEGER NOT NULL,
    "DataHoraMedicao" TIMESTAMPTZ NOT NULL,
    "ValorMedicao" DECIMAL(18, 4) NOT NULL,
    "RecebidoEm" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "IdLote" UUID,
    CONSTRAINT "FK_Medicoes_Sensores" FOREIGN KEY ("IdSensor")
        REFERENCES "Sensores" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Medicoes_IdSensor_DataHoraMedicao"
    ON "Medicoes" ("IdSensor", "DataHoraMedicao");
CREATE INDEX IF NOT EXISTS "IX_Medicoes_IdLote"
    ON "Medicoes" ("IdLote");

-- =====================================================
-- TABELA: VinculosSensorEquipamento
-- =====================================================
CREATE TABLE IF NOT EXISTS "VinculosSensorEquipamento" (
    "Id" SERIAL PRIMARY KEY,
    "IdSensor" INTEGER NOT NULL,
    "IdEquipamento" INTEGER NOT NULL,
    "VinculadoPor" VARCHAR(200) NOT NULL,
    "VinculadoEm" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "EstaAtivo" BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT "FK_VinculosSensorEquipamento_Sensores" FOREIGN KEY ("IdSensor")
        REFERENCES "Sensores" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_VinculosSensorEquipamento_Equipamentos" FOREIGN KEY ("IdEquipamento")
        REFERENCES "Equipamentos" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_VinculosSensorEquipamento_IdSensor_IdEquipamento_EstaAtivo"
    ON "VinculosSensorEquipamento" ("IdSensor", "IdEquipamento", "EstaAtivo");

-- =====================================================
-- TABELA: RegrasAlerta
-- =====================================================
CREATE TABLE IF NOT EXISTS "RegrasAlerta" (
    "Id" SERIAL PRIMARY KEY,
    "IdSensor" INTEGER NOT NULL,
    "TipoRegra" INTEGER NOT NULL,
    "LimiteMinimo" DECIMAL(18, 4) NOT NULL,
    "LimiteMaximo" DECIMAL(18, 4) NOT NULL,
    "ContagemConsecutiva" INTEGER,
    "TamanhoJanelaMedia" INTEGER,
    "MargemErro" INTEGER,
    "EmailNotificacao" VARCHAR(200) NOT NULL,
    "EstaAtivo" BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT "FK_RegrasAlerta_Sensores" FOREIGN KEY ("IdSensor")
        REFERENCES "Sensores" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_RegrasAlerta_IdSensor_EstaAtivo"
    ON "RegrasAlerta" ("IdSensor", "EstaAtivo");

-- =====================================================
-- TABELA: EstadosAlerta
-- =====================================================
CREATE TABLE IF NOT EXISTS "EstadosAlerta" (
    "Id" SERIAL PRIMARY KEY,
    "IdSensor" INTEGER NOT NULL,
    "IdRegraAlerta" INTEGER NOT NULL,
    "ContagemConsecutiva" INTEGER NOT NULL DEFAULT 0,
    "JsonMedicoesRecentes" JSONB NOT NULL DEFAULT '[]'::jsonb,
    "UltimaAtualizacao" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_EstadosAlerta_Sensores" FOREIGN KEY ("IdSensor")
        REFERENCES "Sensores" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EstadosAlerta_RegrasAlerta" FOREIGN KEY ("IdRegraAlerta")
        REFERENCES "RegrasAlerta" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_EstadosAlerta_IdSensor_IdRegraAlerta"
    ON "EstadosAlerta" ("IdSensor", "IdRegraAlerta");

-- =====================================================
-- TABELA: HistoricoAlertas
-- =====================================================
CREATE TABLE IF NOT EXISTS "HistoricoAlertas" (
    "Id" BIGSERIAL PRIMARY KEY,
    "IdSensor" INTEGER NOT NULL,
    "IdRegraAlerta" INTEGER NOT NULL,
    "TipoRegra" INTEGER NOT NULL,
    "MotivoDisparo" VARCHAR(500) NOT NULL,
    "ValorDisparo" DECIMAL(18, 4),
    "DisparadoEm" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "EmailEnviado" BOOLEAN NOT NULL DEFAULT FALSE,
    "EmailEnviadoEm" TIMESTAMPTZ,
    CONSTRAINT "FK_HistoricoAlertas_Sensores" FOREIGN KEY ("IdSensor")
        REFERENCES "Sensores" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_HistoricoAlertas_RegrasAlerta" FOREIGN KEY ("IdRegraAlerta")
        REFERENCES "RegrasAlerta" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_HistoricoAlertas_DisparadoEm"
    ON "HistoricoAlertas" ("DisparadoEm");
CREATE INDEX IF NOT EXISTS "IX_HistoricoAlertas_IdSensor_DisparadoEm"
    ON "HistoricoAlertas" ("IdSensor", "DisparadoEm");

-- =====================================================
-- COMENTÁRIOS NAS TABELAS
-- =====================================================
COMMENT ON TABLE "Setores" IS 'Setores organizacionais da empresa';
COMMENT ON TABLE "Sensores" IS 'Sensores IoT cadastrados no sistema';
COMMENT ON TABLE "Equipamentos" IS 'Equipamentos monitorados';
COMMENT ON TABLE "Medicoes" IS 'Medições coletadas dos sensores';
COMMENT ON TABLE "VinculosSensorEquipamento" IS 'Relacionamento entre sensores e equipamentos';
COMMENT ON TABLE "RegrasAlerta" IS 'Regras de alerta configuradas por sensor';
COMMENT ON TABLE "EstadosAlerta" IS 'Estado atual da avaliação de alertas';
COMMENT ON TABLE "HistoricoAlertas" IS 'Histórico de alertas disparados';

-- =====================================================
-- FIM DO SCRIPT
-- =====================================================
