using FI.AtividadeEntrevista.BLL;
using WebAtividadeEntrevista.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FI.AtividadeEntrevista.DML;

namespace WebAtividadeEntrevista.Controllers
{
    public class ClienteController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }


        public ActionResult Incluir()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Incluir(ClienteModel model)
        {
            BoCliente bo = new BoCliente();

            ValidarCliente(model, bo);

            if (!this.ModelState.IsValid)
            {
                return RetornarErros();
            }
            else
            {
                try
                {
                    model.Id = bo.Incluir(MapearParaCliente(model));
                }
                catch (RegraNegocioException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    return RetornarErros();
                }

                return Json("Cadastro efetuado com sucesso");
            }
        }

        [HttpPost]
        public JsonResult Alterar(ClienteModel model)
        {
            BoCliente bo = new BoCliente();

            ValidarCliente(model, bo);

            if (!this.ModelState.IsValid)
            {
                return RetornarErros();
            }
            else
            {
                try
                {
                    bo.Alterar(MapearParaCliente(model));
                }
                catch (RegraNegocioException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    return RetornarErros();
                }

                return Json("Cadastro alterado com sucesso");
            }
        }

        [HttpGet]
        public ActionResult Alterar(long id)
        {
            BoCliente bo = new BoCliente();
            Cliente cliente = bo.Consultar(id);
            Models.ClienteModel model = null;

            if (cliente != null)
            {
                model = new ClienteModel()
                {
                    Id = cliente.Id,
                    CEP = cliente.CEP,
                    Cidade = cliente.Cidade,
                    Email = cliente.Email,
                    Estado = cliente.Estado,
                    Logradouro = cliente.Logradouro,
                    Nacionalidade = cliente.Nacionalidade,
                    Nome = cliente.Nome,
                    Sobrenome = cliente.Sobrenome,
                    Telefone = cliente.Telefone,
                    CPF = ValidadorCpf.Formatar(cliente.CPF),
                    Beneficiarios = (cliente.Beneficiarios ?? new List<Beneficiario>())
                        .Select(b => new BeneficiarioModel()
                        {
                            Id = b.Id,
                            CPF = ValidadorCpf.Formatar(b.CPF),
                            Nome = b.Nome
                        }).ToList()
                };
            }

            return View(model);
        }

        [HttpPost]
        public JsonResult ClienteList(int jtStartIndex = 0, int jtPageSize = 0, string jtSorting = null)
        {
            try
            {
                int qtd = 0;
                string campo = string.Empty;
                string crescente = string.Empty;
                string[] array = jtSorting.Split(' ');

                if (array.Length > 0)
                    campo = array[0];

                if (array.Length > 1)
                    crescente = array[1];

                List<Cliente> clientes = new BoCliente().Pesquisa(jtStartIndex, jtPageSize, campo, crescente.Equals("ASC", StringComparison.InvariantCultureIgnoreCase), out qtd);

                //Return result to jTable
                return Json(new { Result = "OK", Records = clientes, TotalRecordCount = qtd });
            }
            catch (Exception ex)
            {
                return Json(new { Result = "ERROR", Message = ex.Message });
            }
        }

        /// <summary>
        /// Valida o CPF do cliente (formato, digito verificador e duplicidade) e os CPFs dos beneficiarios
        /// (formato, digito verificador e duplicidade dentro do mesmo cliente)
        /// </summary>
        private void ValidarCliente(ClienteModel model, BoCliente bo)
        {
            if (model == null)
                return;

            if (model.Beneficiarios == null)
                model.Beneficiarios = new List<BeneficiarioModel>();

            if (!string.IsNullOrWhiteSpace(model.CPF))
            {
                if (!ValidadorCpf.Validar(model.CPF))
                    ModelState.AddModelError("CPF", "O CPF informado é inválido.");
                else if (bo.VerificarExistencia(ValidadorCpf.SomenteDigitos(model.CPF), model.Id))
                    ModelState.AddModelError("CPF", "Já existe um cliente cadastrado com o CPF informado.");
            }

            HashSet<string> cpfsInformados = new HashSet<string>();
            foreach (BeneficiarioModel beneficiario in model.Beneficiarios)
            {
                if (beneficiario == null || string.IsNullOrWhiteSpace(beneficiario.CPF))
                    continue; // obrigatoriedade ja validada pelo [Required]

                if (!ValidadorCpf.Validar(beneficiario.CPF))
                    ModelState.AddModelError("Beneficiarios", "O CPF do beneficiário " + beneficiario.CPF + " é inválido.");
                else if (!cpfsInformados.Add(ValidadorCpf.SomenteDigitos(beneficiario.CPF)))
                    ModelState.AddModelError("Beneficiarios", "O CPF " + beneficiario.CPF + " está duplicado na lista de beneficiários.");
            }
        }

        private static Cliente MapearParaCliente(ClienteModel model)
        {
            return new Cliente()
            {
                Id = model.Id,
                CEP = model.CEP,
                Cidade = model.Cidade,
                Email = model.Email,
                Estado = model.Estado,
                Logradouro = model.Logradouro,
                Nacionalidade = model.Nacionalidade,
                Nome = model.Nome,
                Sobrenome = model.Sobrenome,
                Telefone = model.Telefone,
                CPF = ValidadorCpf.SomenteDigitos(model.CPF),
                Beneficiarios = model.Beneficiarios
                    .Where(b => b != null)
                    .Select(b => new Beneficiario()
                    {
                        Id = b.Id,
                        CPF = ValidadorCpf.SomenteDigitos(b.CPF),
                        Nome = b.Nome.Trim()
                    }).ToList()
            };
        }

        private JsonResult RetornarErros()
        {
            List<string> erros = (from item in ModelState.Values
                                  from error in item.Errors
                                  select error.ErrorMessage).ToList();

            Response.StatusCode = 400;
            return Json(string.Join(Environment.NewLine, erros));
        }
    }
}