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
