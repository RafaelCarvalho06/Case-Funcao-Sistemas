namespace FI.AtividadeEntrevista.DML
{
    /// <summary>
    /// Classe de beneficiario que representa o registro na tabela BENEFICIARIOS do Banco de Dados
    /// </summary>
    public class Beneficiario
    {
        /// <summary>
        /// Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// CPF (somente digitos quando vem do/para o banco de dados)
        /// </summary>
        public string CPF { get; set; }

        /// <summary>
        /// Nome
        /// </summary>
        public string Nome { get; set; }

        /// <summary>
        /// Id do cliente ao qual o beneficiario pertence
        /// </summary>
        public long IdCliente { get; set; }
    }
}
