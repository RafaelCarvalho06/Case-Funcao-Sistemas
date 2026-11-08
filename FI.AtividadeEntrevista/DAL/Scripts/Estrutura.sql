/*
  Estrutura do banco (idempotente - pode ser executado quantas vezes for necessario).
  Executado automaticamente no Application_Start, ou manualmente (SSMS / VS) separando os lotes por GO.

  - CLIENTES.CPF (VARCHAR(14), unico quando preenchido)
  - BENEFICIARIOS (ID, CPF, NOME, IDCLIENTE), CPF unico por cliente
  O CPF e gravado somente com digitos (ex.: 12345678909); a mascara e apenas visual.
*/

IF COL_LENGTH('dbo.CLIENTES', 'CPF') IS NULL
    ALTER TABLE dbo.CLIENTES ADD CPF VARCHAR(14) NULL
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_CLIENTES_CPF' AND object_id = OBJECT_ID('dbo.CLIENTES'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_CLIENTES_CPF ON dbo.CLIENTES (CPF) WHERE CPF IS NOT NULL
GO

-- Se existir uma tabela BENEFICIARIOS vazia e fora do padrao (sem ID identity), ela e recriada
IF OBJECT_ID('dbo.BENEFICIARIOS', 'U') IS NOT NULL
BEGIN
    IF ISNULL(COLUMNPROPERTY(OBJECT_ID('dbo.BENEFICIARIOS'), 'ID', 'IsIdentity'), 0) = 0
       AND NOT EXISTS (SELECT 1 FROM dbo.BENEFICIARIOS)
        DROP TABLE dbo.BENEFICIARIOS
END
GO

IF OBJECT_ID('dbo.BENEFICIARIOS', 'U') IS NULL
    CREATE TABLE dbo.BENEFICIARIOS
    (
        ID          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BENEFICIARIOS PRIMARY KEY,
        CPF         VARCHAR(14) NOT NULL,
        NOME        VARCHAR(50) NOT NULL,
        IDCLIENTE   BIGINT      NOT NULL CONSTRAINT FK_BENEFICIARIOS_CLIENTES REFERENCES dbo.CLIENTES (ID) ON DELETE CASCADE
    )
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_BENEFICIARIOS_IDCLIENTE_CPF' AND object_id = OBJECT_ID('dbo.BENEFICIARIOS'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_BENEFICIARIOS_IDCLIENTE_CPF ON dbo.BENEFICIARIOS (IDCLIENTE, CPF)
GO
