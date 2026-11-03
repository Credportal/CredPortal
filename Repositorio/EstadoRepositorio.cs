using Analise.Data;
using Analise.Enuns;
using Analise.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;

namespace Analise.Repositorio
{
    public class EstadoRepositorio : IEstadoRepositorio
    {
        private readonly BancoContext _context;
        public EstadoRepositorio(BancoContext bancoContext)
        {
            this._context = bancoContext;
        }
        public EstadoModel ListarPorId(int id)
        {
            return _context.estados.FirstOrDefault(x => x.Id == id);
        }
        public EstadoModel Adicionar(EstadoModel registo)
        {
            registo.DataCadastro = DateTime.Now;
            _context.estados.Add(registo);
            _context.SaveChanges();
            return registo;
        }
        public EstadoModel Actualizar(EstadoModel registo)
        {
            EstadoModel registoDB = ListarPorId(registo.Id);
            if (registoDB == null) throw new System.Exception("Erro na actualização do registo!");
            registoDB.Estado = registo.Estado;

            _context.estados.Update(registoDB);
            _context.SaveChanges();
            return registoDB;
        }
        public EstadoModel ActualizarRegisto(EstadoModel registo)
        {
            EstadoModel registoDB = ListarPorId(registo.Id);
            if (registoDB == null) throw new System.Exception("Erro na actualização do registo!");
            registoDB.Estado = registo.Estado;

            _context.estados.Update(registoDB);
            _context.SaveChanges();
            return registoDB;
        }

        public List<EstadoModel> BuscarTodos()
        {
            return _context.estados.ToList();
        }
    }
}
