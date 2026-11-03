using Analise.Data;
using Analise.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;

namespace Analise.Repositorio
{
    public class EntidadeRepositorio : IEntidadeRepositorio
    {
        private readonly BancoContext _context;

        public EntidadeRepositorio(BancoContext bancoContext)
        {
            this._context = bancoContext;
        }

        public EntidadeModel ListarPorId(int id)
        {
            return _context.Entidades.FirstOrDefault(x => x.Id == id);
        }

        public List<EntidadeModel> BuscarTodos()
        {
            return _context.Entidades.ToList();
        }

        public EntidadeModel Adicionar(EntidadeModel entidade)
        {
            entidade.DataCadastro = DateTimeOffset.Now;

            _context.Entidades.Add(entidade);
            _context.SaveChanges();
            return entidade;
        }

        public EntidadeModel Actualizar(EntidadeModel entidade)
        {
            EntidadeModel entidadeDB = ListarPorId(entidade.Id);
            if (entidadeDB == null)
                throw new Exception("Entidade não encontrada para actualização!");

            // Atualização de todos os campos da entidade
            entidadeDB.Nome = entidade.Nome;
            entidadeDB.Nuit = entidade.Nuit;
            entidadeDB.Aniversario = entidade.Aniversario;
            entidadeDB.Genero = entidade.Genero;
            entidadeDB.EstadoCivil = entidade.EstadoCivil;
            entidadeDB.Naturalidade = entidade.Naturalidade;
            entidadeDB.TipoCliente = entidade.TipoCliente;
            entidadeDB.Documento = entidade.Documento;
            entidadeDB.NrDocumento = entidade.NrDocumento;
            entidadeDB.LocalEmissao = entidade.LocalEmissao;
            entidadeDB.Emissao = entidade.Emissao;
            entidadeDB.Validade = entidade.Validade;
            entidadeDB.Telefone = entidade.Telefone;
            entidadeDB.Alternativo = entidade.Alternativo;
            entidadeDB.Email = entidade.Email;
            entidadeDB.Endereco = entidade.Endereco;
            entidadeDB.Profissao = entidade.Profissao;
            entidadeDB.Cargo = entidade.Cargo;
            entidadeDB.LocalTrabalho = entidade.LocalTrabalho;
            entidadeDB.Pai = entidade.Pai;
            entidadeDB.Mae = entidade.Mae;
            entidadeDB.Conjuge = entidade.Conjuge;
            entidadeDB.TelefoneConjuge = entidade.TelefoneConjuge;
            entidadeDB.Ativo = entidade.Ativo;

            _context.Entidades.Update(entidadeDB);
            _context.SaveChanges();

            return entidadeDB;
        }
    }
}