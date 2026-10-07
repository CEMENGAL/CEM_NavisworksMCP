// CEM_NavisIAModeler/Controllers/VisibilityController.cs
using System.Threading;
using System.Web.Script.Serialization;
using CEM_NavisIAModeler.Contracts;
using CEM_NavisIAModeler.Infrastructure;
using CEM_NavisIAModeler.Services;
using CEM_NavisIAModeler.Services.Implementations;
using static CEM_NavisIAModeler.Infrastructure.ErrorHandlingMiddleware;

namespace CEM_NavisIAModeler.Controllers
{
    /// <summary>
    /// Controller for handling RPC requests related to element visibility.
    /// - Will provide methods to hide, show, or isolate elements in the model.
    /// - Wraps service calls in error handling middleware for consistent responses.
    /// </summary>
    public sealed class VisibilityController
    {
        private readonly IVisibilityService _svc = new VisibilityService();
        private static readonly JavaScriptSerializer _jss = new JavaScriptSerializer();

        
    }
}
