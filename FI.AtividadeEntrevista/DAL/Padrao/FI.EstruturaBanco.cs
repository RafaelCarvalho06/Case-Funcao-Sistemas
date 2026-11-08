using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace FI.AtividadeEntrevista.DAL
{
    /// <summary>
    /// Garante que a estrutura do banco (coluna CPF, tabela BENEFICIARIOS, indices e procedures)
    /// exista, executando os scripts .sql embarcados na assembly. Os scripts sao idempotentes.
    /// </summary>
    internal class EstruturaBanco : AcessoDados
    {
        internal void Garantir()
        {
            Assembly assembly = typeof(EstruturaBanco).Assembly;

            // Primeiro o script de estrutura (tabelas/indices), depois as procedures
            List<string> recursos = assembly.GetManifestResourceNames()
                .Where(n => n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n.IndexOf(".Scripts.Estrutura.", StringComparison.OrdinalIgnoreCase) >= 0 ? 0 : 1)
                .ThenBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (string recurso in recursos)
            {
                string conteudo;
                using (Stream stream = assembly.GetManifestResourceStream(recurso))
                using (StreamReader reader = new StreamReader(stream))
                    conteudo = reader.ReadToEnd();

                // Separa os lotes pelo comando GO (sozinho na linha)
                string[] lotes = Regex.Split(conteudo, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                foreach (string lote in lotes)
                {
                    if (string.IsNullOrWhiteSpace(lote))
                        continue;

                    try
                    {
                        base.ExecutarScript(lote);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            "Falha ao atualizar a estrutura do banco de dados (script '" + recurso + "'): " + ex.Message, ex);
                    }
                }
            }
        }
    }
}
