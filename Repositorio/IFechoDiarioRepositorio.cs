using Analise.Models;
using System;
using System.Threading.Tasks;

namespace Analise.Repositorio
{
    public interface IFechoDiarioRepositorio
    {
        Task<FechoDiarioViewModel> ObterDadosFechoDiarioAsync(DateTime dataReferencia);
        Task FecharDiaEspecificoAsync(DateTime dataReferencia, int usuarioId);
    }
}