using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace FI.WebAtividadeEntrevista
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Garante a coluna CPF, a tabela BENEFICIARIOS e as procedures no banco (idempotente)
            new FI.AtividadeEntrevista.BLL.BoBancoDados().GarantirEstrutura();
        }
    }
}
