-- Retorna uma linha se ja existir outro cliente (ID diferente de @ID) com o mesmo CPF
CREATE OR ALTER PROCEDURE FI_SP_VerificaCliente
	@CPF VARCHAR(14),
	@ID  BIGINT = 0
AS
BEGIN
	SELECT 1 FROM CLIENTES WITH(NOLOCK) WHERE CPF = @CPF AND ID <> ISNULL(@ID, 0)
END
