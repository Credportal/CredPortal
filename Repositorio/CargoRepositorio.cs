using Analise.Data;
using Analise.Enuns;
using Analise.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;

namespace Analise.Repositorio
{
    public class CargoRepositorio : ICargoRepositorio
    {
        private readonly BancoContext _context;
        public CargoRepositorio(BancoContext bancoContext)
        {
            this._context = bancoContext;
        }
        public CargoModel ListarPorId(int id)
        {
            return _context.cargos.FirstOrDefault(x => x.Id == id);
        }
        public CargoModel Adicionar(CargoModel registo)
        {
            registo.DataCadastro = DateTime.Now;
            _context.cargos.Add(registo);
            _context.SaveChanges();
            return registo;
        }
        public CargoModel Actualizar(CargoModel registo)
        {
            CargoModel registoDB = ListarPorId(registo.Id);
            if (registoDB == null) throw new System.Exception("Erro na actualização!");
            registoDB.Cargo = registo.Cargo;

            _context.cargos.Update(registoDB);
            _context.SaveChanges();
            return registoDB;
        }
        public List<CargoModel> BuscarTodos()
        {
            return _context.cargos.ToList();
        }
    }
}
