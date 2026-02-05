-- =====================================================
-- Script de Dados Iniciais (Seed)
-- IoT Sensor Monitoring System
-- =====================================================

-- =====================================================
-- SETORES
-- =====================================================
INSERT INTO "Setores" ("Nome", "Descricao", "EstaAtivo")
VALUES
    ('Chão de Produção', 'Área principal de produção', TRUE),
    ('Armazém', 'Área de armazenamento', TRUE)
ON CONFLICT DO NOTHING;

-- =====================================================
-- SENSORES
-- =====================================================
INSERT INTO "Sensores" ("Id", "Codigo", "Nome", "Descricao", "EstaAtivo", "CriadoEm")
VALUES
    (1, 'SENSOR001', 'Sensor de Temperatura 1', 'Monitor de temperatura do compressor', TRUE, NOW()),
    (2, 'SENSOR002', 'Sensor de Pressão 1', 'Monitor de pressão do compressor', TRUE, NOW()),
    (3, 'SENSOR003', 'Sensor de Temperatura 2', 'Temperatura da unidade de resfriamento', TRUE, NOW())
ON CONFLICT ("Id") DO NOTHING;

-- =====================================================
-- EQUIPAMENTOS
-- =====================================================
INSERT INTO "Equipamentos" ("Nome", "Descricao", "IdSetor", "EstaAtivo")
VALUES
    ('Compressor A', 'Compressor de ar principal', 1, TRUE),
    ('Unidade de Resfriamento B', 'Sistema HVAC de resfriamento', 1, TRUE),
    ('Monitor de Temperatura C', 'Controle de temperatura do armazém', 2, TRUE)
ON CONFLICT DO NOTHING;

-- =====================================================
-- VÍNCULOS SENSOR-EQUIPAMENTO
-- =====================================================
INSERT INTO "VinculosSensorEquipamento" ("IdSensor", "IdEquipamento", "VinculadoPor", "VinculadoEm", "EstaAtivo")
VALUES
    (1, 1, 'system@example.com', NOW(), TRUE),
    (2, 1, 'system@example.com', NOW(), TRUE),
    (3, 2, 'system@example.com', NOW(), TRUE)
ON CONFLICT DO NOTHING;

-- =====================================================
-- REGRAS DE ALERTA
-- =====================================================
-- TipoRegra: 1 = ConsecutivoForaIntervalo, 2 = MediaMargemErro
INSERT INTO "RegrasAlerta" (
    "IdSensor",
    "TipoRegra",
    "LimiteMinimo",
    "LimiteMaximo",
    "ContagemConsecutiva",
    "TamanhoJanelaMedia",
    "MargemErro",
    "EmailNotificacao",
    "EstaAtivo"
)
VALUES
    (1, 1, 1.0, 50.0, 5, NULL, NULL, 'alerts@example.com', TRUE),
    (1, 2, 1.0, 50.0, NULL, 50, 2, 'alerts@example.com', TRUE),
    (2, 1, 1.0, 50.0, 5, NULL, NULL, 'alerts@example.com', TRUE)
ON CONFLICT DO NOTHING;

-- =====================================================
-- FIM DO SCRIPT DE SEED
-- =====================================================
