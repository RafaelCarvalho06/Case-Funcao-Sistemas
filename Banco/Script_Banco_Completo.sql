/*
  ==========================================================================
  SCRIPT COMPLETO DO BANCO - uso MANUAL (opcional)

  A aplicacao ja executa estes mesmos scripts sozinha na primeira execucao
  (Global.asax -> BoBancoDados.GarantirEstrutura). Use este arquivo somente
  se precisar aplicar o banco na mao (veja o README.md, secao "Banco de dados").

  - Idempotente: pode ser executado varias vezes sem causar erro ou perda de dados.
  - Execute conectado ao banco da aplicacao (App_Data\BancoDeDados.mdf).
  - O comando GO separa os lotes (funciona no SSMS e na janela de consulta do Visual Studio).
  - Este arquivo e gerado a partir dos .sql de FI.AtividadeEntrevista\DAL.
  ==========================================================================
*/

-- ---------------------------------------------------------------
-- Estrutura (coluna CPF, tabela BENEFICIARIOS, indices)
-- ---------------------------------------------------------------
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

-- ---------------------------------------------------------------
-- FI_SP_IncClienteV2
-- ---------------------------------------------------------------
CREATE OR ALTER PROCEDURE FI_SP_IncClienteV2
    @NOME          VARCHAR (50) ,
    @SOBRENOME     VARCHAR (255),
    @NACIONALIDADE VARCHAR (50) ,
    @CEP           VARCHAR (9)  ,
    @ESTADO        VARCHAR (2)  ,
    @CIDADE        VARCHAR (50) ,
    @LOGRADOURO    VARCHAR (500),
    @EMAIL         VARCHAR (2079),
    @TELEFONE      VARCHAR (15),
    @CPF           VARCHAR (14),
    @BENEFICIARIOS XML = NULL
AS
BEGIN
    SET NOCOUNT ON
    SET XACT_ABORT ON

    DECLARE @ID BIGINT

    BEGIN TRY
        BEGIN TRANSACTION

        INSERT INTO CLIENTES (NOME, SOBRENOME, NACIONALIDADE, CEP, ESTADO, CIDADE, LOGRADOURO, EMAIL, TELEFONE, CPF)
        VALUES (@NOME, @SOBRENOME, @NACIONALIDADE, @CEP, @ESTADO, @CIDADE, @LOGRADOURO, @EMAIL, @TELEFONE, @CPF)

        SET @ID = SCOPE_IDENTITY()

        IF @BENEFICIARIOS IS NOT NULL
            INSERT INTO BENEFICIARIOS (CPF, NOME, IDCLIENTE)
            SELECT B.value('(CPF)[1]', 'VARCHAR(14)'), B.value('(NOME)[1]', 'VARCHAR(50)'), @ID
            FROM @BENEFICIARIOS.nodes('/Beneficiarios/Beneficiario') AS T(B)

        COMMIT TRANSACTION
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT @ID
END
GO

-- ---------------------------------------------------------------
-- FI_SP_AltCliente
-- ---------------------------------------------------------------
CREATE OR ALTER PROCEDURE FI_SP_AltCliente
    @NOME          VARCHAR (50) ,
    @SOBRENOME     VARCHAR (255),
    @NACIONALIDADE VARCHAR (50) ,
    @CEP           VARCHAR (9)  ,
    @ESTADO        VARCHAR (2)  ,
    @CIDADE        VARCHAR (50) ,
    @LOGRADOURO    VARCHAR (500),
    @EMAIL         VARCHAR (2079),
    @TELEFONE      VARCHAR (15),
    @CPF           VARCHAR (14),
    @Id            BIGINT,
    @BENEFICIARIOS XML = NULL   -- NULL = nao altera os beneficiarios; caso contrario substitui a lista inteira
AS
BEGIN
    SET NOCOUNT ON
    SET XACT_ABORT ON

    BEGIN TRY
        BEGIN TRANSACTION

        UPDATE CLIENTES
        SET
            NOME = @NOME,
            SOBRENOME = @SOBRENOME,
            NACIONALIDADE = @NACIONALIDADE,
            CEP = @CEP,
            ESTADO = @ESTADO,
            CIDADE = @CIDADE,
            LOGRADOURO = @LOGRADOURO,
            EMAIL = @EMAIL,
            TELEFONE = @TELEFONE,
            CPF = @CPF
        WHERE Id = @Id

        IF @BENEFICIARIOS IS NOT NULL
        BEGIN
            DELETE FROM BENEFICIARIOS WHERE IDCLIENTE = @Id

            INSERT INTO BENEFICIARIOS (CPF, NOME, IDCLIENTE)
            SELECT B.value('(CPF)[1]', 'VARCHAR(14)'), B.value('(NOME)[1]', 'VARCHAR(50)'), @Id
            FROM @BENEFICIARIOS.nodes('/Beneficiarios/Beneficiario') AS T(B)
        END

        COMMIT TRANSACTION
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ---------------------------------------------------------------
-- FI_SP_ConsCliente
-- ---------------------------------------------------------------
CREATE OR ALTER PROCEDURE FI_SP_ConsCliente
	@ID BIGINT
AS
BEGIN
	IF(ISNULL(@ID,0) = 0)
		SELECT NOME, SOBRENOME, NACIONALIDADE, CEP, ESTADO, CIDADE, LOGRADOURO, EMAIL, TELEFONE, CPF, ID FROM CLIENTES WITH(NOLOCK)
	ELSE
		SELECT NOME, SOBRENOME, NACIONALIDADE, CEP, ESTADO, CIDADE, LOGRADOURO, EMAIL, TELEFONE, CPF, ID FROM CLIENTES WITH(NOLOCK) WHERE ID = @ID
END
GO

-- ---------------------------------------------------------------
-- FI_SP_PesqCliente
-- ---------------------------------------------------------------
CREATE OR ALTER PROCEDURE FI_SP_PesqCliente
	@iniciarEm int,
	@quantidade int,
	@campoOrdenacao varchar(200),
	@crescente bit	
AS
BEGIN
	DECLARE @SCRIPT NVARCHAR(MAX)
	DECLARE @CAMPOS NVARCHAR(MAX)
	DECLARE @ORDER VARCHAR(50)
	
	IF(@campoOrdenacao = 'EMAIL')
		SET @ORDER =  ' EMAIL '
	ELSE
		SET @ORDER = ' NOME '

	IF(@crescente = 0)
		SET @ORDER = @ORDER + ' DESC'
	ELSE
		SET @ORDER = @ORDER + ' ASC'

	SET @CAMPOS = '@iniciarEm int,@quantidade int'
	SET @SCRIPT = 
	'SELECT ID, NOME, SOBRENOME, NACIONALIDADE, CEP, ESTADO, CIDADE, LOGRADOURO, EMAIL, TELEFONE, CPF FROM
		(SELECT ROW_NUMBER() OVER (ORDER BY ' + @ORDER + ') AS Row, ID, NOME, SOBRENOME, NACIONALIDADE, CEP, ESTADO, CIDADE, LOGRADOURO, EMAIL, TELEFONE, CPF FROM CLIENTES WITH(NOLOCK))
		AS ClientesWithRowNumbers
	WHERE Row > @iniciarEm AND Row <= (@iniciarEm+@quantidade) ORDER BY'
	
	SET @SCRIPT = @SCRIPT + @ORDER
			
	EXECUTE SP_EXECUTESQL @SCRIPT, @CAMPOS, @iniciarEm, @quantidade

	SELECT COUNT(1) FROM CLIENTES WITH(NOLOCK)
END
GO

-- ---------------------------------------------------------------
-- FI_SP_VerificaCliente
-- ---------------------------------------------------------------
-- Retorna uma linha se ja existir outro cliente (ID diferente de @ID) com o mesmo CPF
CREATE OR ALTER PROCEDURE FI_SP_VerificaCliente
	@CPF VARCHAR(14),
	@ID  BIGINT = 0
AS
BEGIN
	SELECT 1 FROM CLIENTES WITH(NOLOCK) WHERE CPF = @CPF AND ID <> ISNULL(@ID, 0)
END
GO

-- ---------------------------------------------------------------
-- FI_SP_DelCliente
-- ---------------------------------------------------------------
CREATE OR ALTER PROCEDURE FI_SP_DelCliente
	@ID BIGINT
AS
BEGIN
	SET XACT_ABORT ON

	BEGIN TRANSACTION
		DELETE BENEFICIARIOS WHERE IDCLIENTE = @ID
		DELETE CLIENTES WHERE ID = @ID
	COMMIT TRANSACTION
END
GO

-- ---------------------------------------------------------------
-- FI_SP_ConsBenef
-- ---------------------------------------------------------------
CREATE OR ALTER PROCEDURE FI_SP_ConsBenef
	@IDCLIENTE BIGINT
AS
BEGIN
	SELECT ID, CPF, NOME, IDCLIENTE FROM BENEFICIARIOS WITH(NOLOCK) WHERE IDCLIENTE = @IDCLIENTE ORDER BY ID
END
GO
