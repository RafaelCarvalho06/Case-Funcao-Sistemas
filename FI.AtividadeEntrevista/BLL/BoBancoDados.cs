namespace FI.AtividadeEntrevista.BLL
{
    public class BoBancoDados
    {
        /// <summary>
        /// Garante a estrutura do banco necessaria para o CPF do cliente e para os beneficiarios
        /// (coluna, tabela, indices e procedures). Pode ser chamado varias vezes.
        /// </summary>
        public void GarantirEstrutura()
        {
            new DAL.EstruturaBanco().Garantir();
        }
    }
}
