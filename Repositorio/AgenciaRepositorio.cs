using Analise.Data;
using Analise.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;

namespace Analise.Repositorio
{
    public class AgenciaRepositorio : IAgenciaRepositorio
    {
        private readonly BancoContext _context;
        public AgenciaRepositorio(BancoContext bancoContext) 
        {
            this._context = bancoContext;
        }
        public AgenciaModel ListarPorId(int id)
        {
            return _context.Agencias.FirstOrDefault(x => x.Id == id);
        }
        public List<AgenciaModel> BuscarTodos()
        {
           return _context.Agencias.ToList();
        }
        public AgenciaModel Adicionar(AgenciaModel agencia)
        {
            agencia.DataCadastro = DateTime.Now;

            _context.Agencias.Add(agencia);
            _context.SaveChanges();
            return agencia;
        }
        public AgenciaModel Actualizar(AgenciaModel agencia)
        {
            AgenciaModel agenciaDB = ListarPorId(agencia.Id);
            if (agenciaDB == null)
                throw new Exception("Erro na actualização!");

            // Atualização dos campos principais
            agenciaDB.Nome = agencia.Nome;
            agenciaDB.Contacto = agencia.Contacto;
            agenciaDB.Endereco = agencia.Endereco;

            _context.Agencias.Update(agenciaDB);
            _context.SaveChanges();

            return agenciaDB;
        }
    }
}
