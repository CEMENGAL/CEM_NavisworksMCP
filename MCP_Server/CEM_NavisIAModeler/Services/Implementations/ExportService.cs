// CEM_NavisIAModeler/Services/Implementations/ExportService.cs
using System.Threading;
using System.Threading.Tasks;
using CEM_NavisIAModeler.Contracts;
using CEM_NavisIAModeler.Services;
using CEM_NavisIAModeler.Services.Backends;
using CEM_NavisIAModeler_Shared;

namespace CEM_NavisIAModeler.Services.Implementations
{
    /// <summary>
    ///  Orchestrates export functions (OBJ/FBX) and CSV dumps via the backend
    ///     (reflection to CEM_NavisIAModeler_Ribbon, otherwise fallback).
    /// </summary>
    public sealed class ExportService : IExportService
    {
        private static IWaabeNavisworksBackend BE => BackendResolver.Instance;

        /*ToDo*/
    }
}
