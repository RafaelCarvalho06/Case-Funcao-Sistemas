using System.Text;
using System.Text.RegularExpressions;

namespace FI.AtividadeEntrevista.BLL
{
    /// <summary>
    /// Utilitario para validacao, normalizacao e formatacao de CPF
    /// </summary>
    public static class ValidadorCpf
    {
        // Aceita 999.999.999-99 ou apenas os 11 digitos (pontuacao opcional)
        private static readonly Regex Padrao = new Regex(@"^\d{3}\.?\d{3}\.?\d{3}-?\d{2}$", RegexOptions.Compiled);

        /// <summary>
        /// Remove tudo o que nao for digito (0-9)
        /// </summary>
        public static string SomenteDigitos(string valor)
        {
            if (string.IsNullOrEmpty(valor))
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            foreach (char c in valor)
                if (c >= '0' && c <= '9')
                    sb.Append(c);

            return sb.ToString();
        }

        /// <summary>
        /// Formata um CPF (11 digitos) como 999.999.999-99. Se nao tiver 11 digitos, devolve o valor original
        /// </summary>
        public static string Formatar(string cpf)
        {
            string digitos = SomenteDigitos(cpf);
            if (digitos.Length != 11)
                return cpf;

            return digitos.Substring(0, 3) + "." + digitos.Substring(3, 3) + "." + digitos.Substring(6, 3) + "-" + digitos.Substring(9, 2);
        }

        /// <summary>
        /// Valida o CPF pelo calculo padrao dos digitos verificadores
        /// </summary>
        public static bool Validar(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf) || !Padrao.IsMatch(cpf.Trim()))
                return false;

            string digitos = SomenteDigitos(cpf);

            // Sequencias iguais (000.000.000-00, 111.111.111-11 ...) passam no calculo, mas sao invalidas
            bool todosIguais = true;
            for (int i = 1; i < digitos.Length; i++)
            {
                if (digitos[i] != digitos[0])
                {
                    todosIguais = false;
                    break;
                }
            }
            if (todosIguais)
                return false;

            int primeiro = CalcularDigito(digitos, 9);
            int segundo = CalcularDigito(digitos, 10);

            return digitos[9] - '0' == primeiro && digitos[10] - '0' == segundo;
        }

        /// <summary>
        /// Calcula o digito verificador considerando os 'quantidade' primeiros digitos
        /// (pesos de quantidade+1 ate 2)
        /// </summary>
        private static int CalcularDigito(string digitos, int quantidade)
        {
            int soma = 0;
            for (int i = 0; i < quantidade; i++)
                soma += (digitos[i] - '0') * (quantidade + 1 - i);

            int resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
    }
}
