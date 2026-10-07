// Controllers/ExportController.cs
using System.Threading;
using System.Web.Script.Serialization;
using CEM_NavisIAModeler.Contracts;
using CEM_NavisIAModeler.Infrastructure;
using CEM_NavisIAModeler.Services;
using CEM_NavisIAModeler.Services.Implementations;
using static CEM_NavisIAModeler.Infrastructure.ErrorHandlingMiddleware;

namespace CEM_NavisIAModeler.Controllers
{

    public sealed class ExportController
    {
        private readonly IExportService _svc = new ExportService();
        private static readonly JavaScriptSerializer _jss = new JavaScriptSerializer();

       /*ToDo*/
    }
}
