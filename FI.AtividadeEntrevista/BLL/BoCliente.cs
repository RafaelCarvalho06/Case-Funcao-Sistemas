using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FI.AtividadeEntrevista.BLL
{
    public class BoCliente
    {
        /// <summary>
        /// Inclui um novo cliente
        /// </summary>
        /// <param name="cliente">Objeto de cliente</param>
        public long Incluir(DML.Cliente cliente)
        {
            DAL.DaoCliente cli = new DAL.DaoCliente();

            try
            {
                return cli.Incluir(cliente);
            }
            catch (SqlException ex) when (CpfDuplicado(ex))
            {
                throw new RegraNegocioException(MensagemCpfDuplicado, ex);
            }
        }

        /// <summary>
        /// Altera um cliente
        /// </summary>
        /// <param name="cliente">Objeto de cliente</param>
        public void Alterar(DML.Cliente cliente)
        {
            DAL.DaoCliente cli = new DAL.DaoCliente();

            try
            {
                cli.Alterar(cliente);
            }
            catch (SqlException ex) when (CpfDuplicado(ex))
            {
                throw new RegraNegocioException(MensagemCpfDuplicado, ex);
            }
        }

        /// <summary>
        /// Consulta o cliente pelo id
        /// </summary>
        /// <param name="id">id do cliente</param>
        /// <returns></returns>
        public DML.Cliente Consultar(long id)
        {
            DAL.DaoCliente cli = new DAL.DaoCliente();
            DML.Cliente cliente = cli.Consultar(id);

            if (cliente != null)
                cliente.Beneficiarios = new DAL.DaoBeneficiario().Consultar(cliente.Id);

            return cliente;
        }

        /// <summary>
        /// Excluir o cliente pelo id
        /// </summary>
        /// <param name="id">id do cliente</param>
        /// <returns></returns>
        public void Excluir(long id)
        {
            DAL.DaoCliente cli = new DAL.DaoCliente();
            cli.Excluir(id);
        }

        /// <summary>
        /// Lista os clientes
        /// </summary>
        public List<DML.Cliente> Listar()
        {
            DAL.DaoCliente cli = new DAL.DaoCliente();
            return cli.Listar();
        }

        /// <summary>
        /// Lista os clientes
        /// </summary>
        public List<DML.Cliente> Pesquisa(int iniciarEm, int quantidade, string campoOrdenacao, bool crescente, out int qtd)
        {
            DAL.DaoCliente cli = new DAL.DaoCliente();
            return cli.Pesquisa(iniciarEm,  quantidade, campoOrdenacao, crescente, out qtd);
        }

        /// <summary>
        /// Verifica se ja existe outro cliente com o CPF informado
        /// </summary>
        /// <param name="CPF">CPF (somente digitos)</param>
        /// <param name="idIgnorar">Id do cliente a ser desconsiderado (na alteracao). Use 0 na inclusao</param>
        /// <returns></returns>
        public bool VerificarExistencia(string CPF, long idIgnorar)
        {
            DAL.DaoCliente cli = new DAL.DaoCliente();
            return cli.VerificarExistencia(CPF, idIgnorar);
        }

        private const string MensagemCpfDuplicado = "Já existe um registro com o CPF informado.";

        /// <summary>
        /// 2601/2627 = violacao de indice unico / chave unica (CPF duplicado, mesmo com concorrencia)
        /// </summary>
        private static bool CpfDuplicado(SqlException ex)
        {
            return ex.Number == 2601 || ex.Number == 2627;
        }
    }
}
