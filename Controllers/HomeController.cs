using Analise.Data;
using Analise.Filters;
using Analise.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Analise.Controllers
{
    public class HomeController : Controller
    {
        private readonly BancoContext _context;
        public HomeController(BancoContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> Index()
        {
            var usuarioId = HttpContext.Session.GetInt32("UsuarioId");
            var agenciaId = HttpContext.Session.GetInt32("AgenciaId");
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);

            // Datas de referência para o mês atual e anterior
            var hoje = DateTime.Today;
            var inicioMesAtual = new DateTime(hoje.Year, hoje.Month, 1);
            var inicioMesSeguinte = inicioMesAtual.AddMonths(1);

            var inicioMesAnterior = inicioMesAtual.AddMonths(-1);

            // 1. Notificações do dia (Vencimento hoje)
            var prestacoesVencemHoje = _context.Amortizacoes
                .Include(a => a.Pedido)
                .ThenInclude(p => p.Entidade)
                .AsEnumerable()
                .Where(a => a.Vencimento.Date == hoje && a.PrincipalPorPagar > 0)
                .ToList();

            // 2. Desembolso do Mês
            decimal totalDesembolsosMesAtual = _context.Pedidos
                .AsEnumerable()
                .Where(p => p.DataDesembolso >= inicioMesAtual && p.DataDesembolso < inicioMesSeguinte)
                .Sum(p => p.Valor);

            decimal totalDesembolsosMesAnterior = _context.Pedidos
                .AsEnumerable()
                .Where(p => p.DataDesembolso >= inicioMesAnterior && p.DataDesembolso < inicioMesAtual)
                .Sum(p => p.Valor);

            decimal variacaoDesembolsos = CalcularVariacao(totalDesembolsosMesAtual, totalDesembolsosMesAnterior);

            // 3. KPI 1: Pagamentos Previstos do Mês (Soma da Prestacao onde Vencimento é do mês atual, pago ou não)
            decimal previstosMesAtual = _context.Amortizacoes
                .AsEnumerable()
                .Where(a => a.Vencimento >= inicioMesAtual && a.Vencimento < inicioMesSeguinte)
                .Sum(a => a.Prestacao);

            decimal previstosMesAnterior = _context.Amortizacoes
                .AsEnumerable()
                .Where(a => a.Vencimento >= inicioMesAnterior && a.Vencimento < inicioMesAtual)
                .Sum(a => a.Prestacao);

            decimal variacaoPrevistos = CalcularVariacao(previstosMesAtual, previstosMesAnterior);

            // 4. KPI 2: Pagamentos Pendentes do Mês (Valores por pagar com Vencimento no mês atual)
            decimal pendentesMesAtual = _context.Amortizacoes
                .AsEnumerable()
                .Where(a => a.Vencimento >= inicioMesAtual && a.Vencimento < inicioMesSeguinte)
                .Sum(a => a.PrincipalPorPagar + a.JuroPorPagar + a.MultaPorPagar);

            decimal pendentesMesAnterior = _context.Amortizacoes
                .AsEnumerable()
                .Where(a => a.Vencimento >= inicioMesAnterior && a.Vencimento < inicioMesAtual)
                .Sum(a => a.PrincipalPorPagar + a.JuroPorPagar + a.MultaPorPagar);

            decimal variacaoPendentes = CalcularVariacao(pendentesMesAtual, pendentesMesAnterior);

            // 5. KPI 3: Carteira Vigente (Soma de Valor - PrincipalPago dos Pedidos)
            decimal carteiraVigenteAtual = _context.Pedidos
                .AsEnumerable()
                .Sum(p => p.Valor - p.CapitalPago);

            decimal carteiraVigenteAnterior = _context.Pedidos
                .AsEnumerable()
                .Where(p => p.DataCadastro < inicioMesAtual)
                .Sum(p => p.Valor - p.CapitalPago);

            decimal variacaoCarteiraVigente = CalcularVariacao(carteiraVigenteAtual, carteiraVigenteAnterior);

            // 6. KPI 4: Carteira em Mora (Soma de CapitalPorPagar onde DiasAtraso > 0)
            var amortizacoesEmMora = _context.Amortizacoes
                .AsEnumerable()
                .Where(a => a.DiasAtraso > 0 && a.PrincipalPorPagar > 0)
                .ToList();

            decimal totalCarteiraMora = amortizacoesEmMora.Sum(a => a.PrincipalPorPagar);
            int quantidadeCobrancasMora = amortizacoesEmMora.Count;

            // 🔹 1. Total de clientes únicos com histórico de pedidos (todas as entidades cadastradas nos pedidos)
            int totalClientesGeral = _context.Pedidos
                .AsEnumerable()
                .Select(p => p.EntidadeId)
                .Distinct()
                .Count();

            // 🔹 2. Total de clientes únicos com saldo vigente (Valor - PrincipalPago > 0)
            int totalClientesAtivos = _context.Pedidos
                .AsEnumerable()
                .Where(p => (p.Valor - p.CapitalPago) > 0)
                .Select(p => p.EntidadeId)
                .Distinct()
                .Count();

            // Passar para a ViewBag
            ViewBag.ClientesTotaisCount = totalClientesGeral;
            ViewBag.ClientesVigentesCount = totalClientesAtivos;

            // ViewBags
            ViewBag.UsuarioId = usuarioId;
            ViewBag.AgenciaId = agenciaId;
            ViewBag.Perfil = usuario?.Perfil;

            ViewBag.PrestacoesHoje = prestacoesVencemHoje;
            ViewBag.TotalNotificacoesHoje = prestacoesVencemHoje.Count;

            ViewBag.TotalDesembolsosMes = totalDesembolsosMesAtual;
            ViewBag.VariacaoDesembolsos = variacaoDesembolsos;

            ViewBag.PagamentosPrevistosMes = previstosMesAtual;
            ViewBag.VariacaoPrevistos = variacaoPrevistos;

            ViewBag.PagamentosPendentesMes = pendentesMesAtual;
            ViewBag.VariacaoPendentes = variacaoPendentes;

            ViewBag.CarteiraVigente = carteiraVigenteAtual;
            ViewBag.VariacaoCarteiraVigente = variacaoCarteiraVigente;

            ViewBag.CarteiraEmMora = totalCarteiraMora;
            ViewBag.QuantidadeCobrancasMora = quantidadeCobrancasMora;



            // 🔹 Dados para o Gráfico de Evolução da Carteira Vigente (Agrupado por Mês)
            var evolucaoCarteira = _context.Amortizacoes
                .AsEnumerable()
                .Where(a => (a.PrincipalPorPagar + a.JuroPorPagar) > 0)
                .GroupBy(a => new { a.Vencimento.Year, a.Vencimento.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new {
                    Mes = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Valor = g.Sum(x => x.PrincipalPorPagar + x.JuroPorPagar + x.MultaPorPagar)
                })
                .ToList();

            ViewBag.CarteiraLabels = System.Text.Json.JsonSerializer.Serialize(evolucaoCarteira.Select(x => x.Mes));
            ViewBag.CarteiraValores = System.Text.Json.JsonSerializer.Serialize(evolucaoCarteira.Select(x => x.Valor));

            // 🔹 Dados para o Gráfico de Operações Chave (Os 5 KPIs principais)
            var operacoesChave = new[]
                        {
                new { Label = "Desembolso do Mês", Valor = totalDesembolsosMesAtual },
                new { Label = "Pagamentos Previstos", Valor = previstosMesAtual },
                new { Label = "Pagamentos Pendentes", Valor = pendentesMesAtual },
                new { Label = "Carteira Vigente", Valor = carteiraVigenteAtual },
                new { Label = "Carteira em Mora", Valor = totalCarteiraMora }
            };

            ViewBag.OperacoesChaveLabels = System.Text.Json.JsonSerializer.Serialize(operacoesChave.Select(x => x.Label));
            ViewBag.OperacoesChaveValores = System.Text.Json.JsonSerializer.Serialize(operacoesChave.Select(x => x.Valor));

            // Vencimentos Próximos
            var vencimentosProximos = _context.Amortizacoes
                .Include(a => a.Pedido)
                .ThenInclude(p => p.Entidade)
                .AsEnumerable()
                .Where(a => a.PrincipalPorPagar > 0 && a.Vencimento.Date >= hoje)
                .OrderBy(a => a.Vencimento)
                .Take(5)
                .ToList();

            // Pagamentos Pendentes
            var pagamentosPendentes = _context.Amortizacoes
                .Include(a => a.Pedido)
                .ThenInclude(p => p.Entidade)
                .AsEnumerable()
                .Where(a => a.PrincipalPorPagar > 0 && a.Vencimento.Date < hoje)
                .OrderBy(a => a.Vencimento)
                .Take(5)
                .ToList();

            ViewBag.VencimentosProximos = vencimentosProximos;
            ViewBag.PagamentosPendentes = pagamentosPendentes;

            return View();
        }

        // Função auxiliar para calcular variação percentual
        private static decimal CalcularVariacao(decimal atual, decimal anterior)
        {
            if (anterior > 0)
                return ((atual - anterior) / anterior) * 100;
            return atual > 0 ? 100 : 0;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}