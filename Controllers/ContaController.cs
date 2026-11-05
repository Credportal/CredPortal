using Analise.Filters;
using Analise.Helper;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Analise.Controllers
{
    [PaginaParaUsuarioLogado]
    public class ContaController : Controller
    {
        private readonly IContaRepositorio _contaRepositorio;
        private readonly IMovimentoInternoRepositorio _movimentoRepositorio;
        private readonly ISessao _sessao;

        public ContaController(IContaRepositorio contaRepositorio, IMovimentoInternoRepositorio movimentoRepositorio, ISessao sessao)
        {
            _contaRepositorio = contaRepositorio;
            _movimentoRepositorio = movimentoRepositorio;
            _sessao = sessao;
        }

        // GET: /Conta/Ver
        public async Task<IActionResult> Ver()
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var viewModel = new ContaViewModel
            {
                ListaContas = await _contaRepositorio.BuscarTodasAsync()
            };

            return View(viewModel);
        }

        // GET: /Conta/Criar
        public IActionResult Criar()
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            return View();
        }

        // POST: /Conta/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(string banco, string conta, string nib, string titular)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            if (string.IsNullOrWhiteSpace(banco) || string.IsNullOrWhiteSpace(conta) ||
                string.IsNullOrWhiteSpace(nib) || string.IsNullOrWhiteSpace(titular))
            {
                TempData["MensagemErro"] = "Por favor, preencha todos os campos obrigatórios da conta bancária.";
                var modelErro = new ContaModel { Banco = banco, Conta = conta, NIB = nib, Titular = titular };
                return View(modelErro);
            }

            var novaConta = new ContaModel
            {
                Banco = banco,
                Conta = conta,
                NIB = nib,
                Titular = titular,
                UsuarioId = usuarioLogado.Id,
                DataCadastro = DateTimeOffset.Now
            };

            await _contaRepositorio.AdicionarAsync(novaConta);
            TempData["MensagemSucesso"] = "Conta bancária registada com sucesso!";
            return RedirectToAction("Ver");
        }

        // GET: /Conta/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var conta = await _contaRepositorio.BuscarPorIdAsync(id);
            if (conta == null)
            {
                TempData["MensagemErro"] = "Conta bancária não encontrada.";
                return RedirectToAction("Ver");
            }

            return View(conta);
        }

        // POST: /Conta/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, string banco, string conta, string nib, string titular)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            if (string.IsNullOrWhiteSpace(banco) || string.IsNullOrWhiteSpace(conta) ||
                string.IsNullOrWhiteSpace(nib) || string.IsNullOrWhiteSpace(titular))
            {
                TempData["MensagemErro"] = "Por favor, preencha todos os campos obrigatórios.";
                var modelErro = new ContaModel { Id = id, Banco = banco, Conta = conta, NIB = nib, Titular = titular };
                return View(modelErro);
            }

            var contaDB = await _contaRepositorio.BuscarPorIdAsync(id);
            if (contaDB == null)
            {
                TempData["MensagemErro"] = "Conta bancária não encontrada para atualização.";
                return RedirectToAction("Ver");
            }

            contaDB.Banco = banco;
            contaDB.Conta = conta;
            contaDB.NIB = nib;
            contaDB.Titular = titular;

            await _contaRepositorio.AtualizarAsync(contaDB);
            TempData["MensagemSucesso"] = "Conta bancária atualizada com sucesso!";
            return RedirectToAction("Ver");
        }

        // ==========================================
        // MÉTODOS DE OPERAÇÕES BANCÁRIAS (DEPÓSITOS E TRANSFERÊNCIAS)
        // ==========================================

        // GET: /Conta/Operacoes
        public async Task<IActionResult> Operacoes()
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var viewModel = new OperacoesViewModel
            {
                ListaContas = await _contaRepositorio.BuscarTodasAsync(),
                ListaMovimentos = await _movimentoRepositorio.BuscarTodosAsync()
            };

            return View(viewModel);
        }

        // POST: /Conta/EfetuarDeposito
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EfetuarDeposito(OperacoesViewModel model)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            if (model.NovoDeposito == null || string.IsNullOrWhiteSpace(model.NovoDeposito.Descricao) ||
                model.NovoDeposito.ContaDestinoId <= 0 || model.NovoDeposito.Credito <= 0)
            {
                TempData["MensagemErro"] = "Por favor, preencha todos os campos do depósito corretamente.";
                model.ListaContas = await _contaRepositorio.BuscarTodasAsync();
                model.ListaMovimentos = await _movimentoRepositorio.BuscarTodosAsync();
                return View("Operacoes", model);
            }

            var contaDestino = await _contaRepositorio.BuscarPorIdAsync(model.NovoDeposito.ContaDestinoId.Value);
            if (contaDestino == null)
            {
                TempData["MensagemErro"] = "Conta de destino não encontrada.";
                return RedirectToAction("Operacoes");
            }

            var movimento = new MovimentoInternoModel
            {
                Descricao = model.NovoDeposito.Descricao,
                Credito = model.NovoDeposito.Credito,
                Debito = 0,
                ContaOrigemId = model.NovoDeposito.ContaDestinoId,
                ContaDestinoId = model.NovoDeposito.ContaDestinoId,
                TipoOperacao = "Depósito",
                UsuarioId = usuarioLogado.Id,
                AgenciaId = usuarioLogado.AgenciaId,
                DataCadastro = DateTimeOffset.Now
            };
            await _movimentoRepositorio.AdicionarAsync(movimento);

            contaDestino.Credito += model.NovoDeposito.Credito;
            //contaDestino.Saldo = contaDestino.Credito - contaDestino.Debito;
            await _contaRepositorio.AtualizarAsync(contaDestino);

            TempData["MensagemSucesso"] = "Depósito efetuado com sucesso!";
            return RedirectToAction("Operacoes");
        }

        // POST: /Conta/EfetuarTransferencia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EfetuarTransferencia(OperacoesViewModel model)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            if (model.NovaTransferencia == null || string.IsNullOrWhiteSpace(model.NovaTransferencia.Descricao) ||
                model.NovaTransferencia.ContaOrigemId <= 0 || model.NovaTransferencia.ContaDestinoId <= 0 ||
                model.NovaTransferencia.Credito <= 0)
            {
                TempData["MensagemErro"] = "Por favor, preencha todos os campos da transferência corretamente.";
                model.ListaContas = await _contaRepositorio.BuscarTodasAsync();
                model.ListaMovimentos = await _movimentoRepositorio.BuscarTodosAsync();
                return View("Operacoes", model);
            }

            if (model.NovaTransferencia.ContaOrigemId == model.NovaTransferencia.ContaDestinoId)
            {
                TempData["MensagemErro"] = "A conta de origem e a conta de destino não podem ser a mesma.";
                model.ListaContas = await _contaRepositorio.BuscarTodasAsync();
                model.ListaMovimentos = await _movimentoRepositorio.BuscarTodosAsync();
                return View("Operacoes", model);
            }

            // Buscar as contas envolvidas
            var contaOrigem = await _contaRepositorio.BuscarPorIdAsync(model.NovaTransferencia.ContaOrigemId.Value);
            var contaDestino = await _contaRepositorio.BuscarPorIdAsync(model.NovaTransferencia.ContaDestinoId.Value);

            if (contaOrigem == null || contaDestino == null)
            {
                TempData["MensagemErro"] = "Conta de origem ou destino não encontrada.";
                return RedirectToAction("Operacoes");
            }

            decimal valorTransferencia = model.NovaTransferencia.Credito;

            // Validação de segurança de saldo no backend
            if (contaOrigem.Saldo < valorTransferencia)
            {
                TempData["MensagemErro"] = "A conta de origem não possui saldo suficiente para esta transferência.";
                model.ListaContas = await _contaRepositorio.BuscarTodasAsync();
                model.ListaMovimentos = await _movimentoRepositorio.BuscarTodosAsync();
                return View("Operacoes", model);
            }

            // 1º Movimento: Registo do Débito na Conta Origem
            var movimentoDebito = new MovimentoInternoModel
            {
                Descricao = model.NovaTransferencia.Descricao,
                Debito = valorTransferencia,
                Credito = 0,
                ContaOrigemId = contaOrigem.Id,
                ContaDestinoId = contaDestino.Id,
                TipoOperacao = "Transferência (Débito)",
                UsuarioId = usuarioLogado.Id,
                AgenciaId = usuarioLogado.AgenciaId,
                DataCadastro = DateTimeOffset.Now
            };
            await _movimentoRepositorio.AdicionarAsync(movimentoDebito);

            // 2º Movimento: Registo do Crédito na Conta Destino
            var movimentoCredito = new MovimentoInternoModel
            {
                Descricao = model.NovaTransferencia.Descricao,
                Credito = valorTransferencia,
                Debito = 0,
                ContaOrigemId = contaOrigem.Id,
                ContaDestinoId = contaDestino.Id,
                TipoOperacao = "Transferência (Crédito)",
                UsuarioId = usuarioLogado.Id,
                AgenciaId = usuarioLogado.AgenciaId,
                DataCadastro = DateTimeOffset.Now
            };
            await _movimentoRepositorio.AdicionarAsync(movimentoCredito);

            // Atualizar Conta Origem (Aumenta o Débito, Recalcula o Saldo)
            contaOrigem.Debito += valorTransferencia;
            //contaOrigem.Saldo = contaOrigem.Credito - contaOrigem.Debito;
            await _contaRepositorio.AtualizarAsync(contaOrigem);

            // Atualizar Conta Destino (Aumenta o Crédito, Recalcula o Saldo)
            contaDestino.Credito += valorTransferencia;
            //contaDestino.Saldo = contaDestino.Credito - contaDestino.Debito;
            await _contaRepositorio.AtualizarAsync(contaDestino);

            TempData["MensagemSucesso"] = "Transferência efetuada com sucesso!";
            return RedirectToAction("Operacoes");
        }
    }
}