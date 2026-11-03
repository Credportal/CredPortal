using Analise.Data;
using Analise.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Analise.Repositorio
{
    public class ConfiguracaoRepositorio : IConfiguracaoRepositorio
    {
        private readonly BancoContext _context;

        public ConfiguracaoRepositorio(BancoContext context)
        {
            _context = context;
        }

        public async Task<ConfiguracaoModel> ObterConfiguracaoAsync()
        {
            // Procura a primeira configuração na base de dados
            var config = await _context.Configuracoes.FirstOrDefaultAsync();

            // Se não existir nenhuma, cria uma nova com os valores predefinidos (ex: Nael Microcrédito)
            if (config == null)
            {
                config = new ConfiguracaoModel();
                _context.Configuracoes.Add(config);
                await _context.SaveChangesAsync();
            }

            return config;
        }

        public async Task AtualizarAsync(ConfiguracaoModel configuracao)
        {
            _context.Configuracoes.Update(configuracao);
            await _context.SaveChangesAsync();
        }
    }
}