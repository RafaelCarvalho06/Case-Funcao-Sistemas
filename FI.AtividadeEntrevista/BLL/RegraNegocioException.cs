using System;

namespace FI.AtividadeEntrevista.BLL
{
    /// <summary>
    /// Excecao lancada quando uma regra de negocio e violada (mensagem pode ser exibida ao usuario)
    /// </summary>
    public class RegraNegocioException : Exception
    {
        public RegraNegocioException(string mensagem) : base(mensagem)
        {
        }

        public RegraNegocioException(string mensagem, Exception interna) : base(mensagem, interna)
        {
        }
    }
}
