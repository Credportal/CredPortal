using Analise.Models;
using Microsoft.EntityFrameworkCore;

namespace Analise.Data
{
    public class BancoContext :DbContext
    {
        public BancoContext(DbContextOptions<BancoContext> options) : base(options)
        {

        }
        
        public DbSet<AgenciaModel> Agencias { get; set; }
        public DbSet<EstadoModel> estados { get; set; }
        public DbSet<CargoModel> cargos { get; set; }
        public DbSet<UsuarioModel> Usuarios { get; set; }
        public DbSet<EntidadeModel> Entidades { get; set; }
        public DbSet<PedidoModel> Pedidos { get; set; }
        public DbSet<GarantiaModel> Garantias { get; set; }
        public DbSet<IntervenienteModel> Intervenientes { get; set; }
        public DbSet<ConfiguracaoModel> Configuracoes { get; set; }
        public DbSet<ContaModel> Contas { get; set; }
        public DbSet<MovimentoInternoModel> MovimentosInternos { get; set; }
        public DbSet<MovimentoExternoModel> MovimentosExternos { get; set; }
        public DbSet<CustosAdmModel> CustosAdm { get; set; }
        public DbSet<AmortizacaoModel> Amortizacoes { get; set; }
        public DbSet<DespesaModel> Despesas { get; set; }
        public DbSet<FechoDiarioRegisto> FechosDiariosRegistos { get; set; }
        public DbSet<FechoDiarioConta> FechosDiariosContas { get; set; }
        public DbSet<FechoDiarioGestor> FechosDiariosGestores { get; set; }
        public DbSet<DiaControloModel> DiasControlo { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.ApplyConfiguration(new RegistoMap());
            base.OnModelCreating(modelBuilder);
        }
    }
}
