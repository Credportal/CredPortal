using Analise.Data;
using Analise.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;

namespace Analise.Repositorio
{
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly BancoContext _context;
        public UsuarioRepositorio(BancoContext bancoContext) 
        {
            this._context = bancoContext;
        }
        public UsuarioModel BuscarLogin(string login)
        {
            return _context.Usuarios.FirstOrDefault(x => x.Login.ToUpper() == login.ToUpper());
        }
        public UsuarioModel BuscarPorEmailELogin(string email, string login)
        {
            return _context.Usuarios.FirstOrDefault(x => x.Email.ToUpper() == email.ToUpper() && x.Login.ToUpper() == login.ToUpper());
        }
        public UsuarioModel ListarPorId(int id)
        {
            return _context.Usuarios.FirstOrDefault(x => x.Id == id);
        }
        public List<UsuarioModel> BuscarTodos()
        {
            return _context.Usuarios
                     .Include(u => u.Agencia) // inclui a agência relacionada
                     .ToList();
        }
        public UsuarioModel Adicionar(UsuarioModel usuario)
        {
            usuario.DataCadastro = DateTime.Now;
            usuario.SetSenhaHash();
            _context.Usuarios.Add(usuario);
            _context.SaveChanges();
            return usuario;
        }
        public UsuarioModel Actualizar(UsuarioModel usuario)
        {
            UsuarioModel usuarioDB = ListarPorId(usuario.Id);
            if (usuarioDB == null) throw new System.Exception("Erro na actualização do usuário!");
            usuarioDB.Nome = usuario.Nome;
            usuarioDB.Login = usuario.Login;
            usuarioDB.Email = usuario.Email;
            usuarioDB.Perfil = usuario.Perfil;
            usuarioDB.CargoId = usuario.CargoId;

            usuarioDB.Criar = usuario.Criar;
            usuarioDB.Editar = usuario.Editar;
            usuarioDB.Visualizar = usuario.Visualizar;
            usuarioDB.Administrar = usuario.Administrar;

            usuarioDB.DataActualizacao = DateTime.Now;

            _context.Usuarios.Update(usuarioDB);
            _context.SaveChanges();
            return usuarioDB;
        }
        public UsuarioModel AlterarSenha(AlterarSenhaModel alterarSenhaModel)
        {
            UsuarioModel usuarioDB = ListarPorId(alterarSenhaModel.Id);
            if (usuarioDB == null) throw new System.Exception("Erro na alteração da senha, usuário não encontrado!");

            if (!usuarioDB.SenhaValida(alterarSenhaModel.SenhaActual)) throw new System.Exception("A senha actual não confere!");

            if (usuarioDB.SenhaValida(alterarSenhaModel.NovaSenha)) throw new System.Exception("A nova senha deve ser diferente da senha actual");

            usuarioDB.SetNovaSenha(alterarSenhaModel.NovaSenha);
            usuarioDB.DataActualizacao=DateTime.Now;

            _context.Usuarios.Update(usuarioDB);
            _context.SaveChanges();

            return usuarioDB;
        }
        public bool Apagar(int id)
        {
            UsuarioModel usuarioDB = ListarPorId(id);

            if (usuarioDB == null) throw new System.Exception("Houve um erro ao excluir o usuário!");

            _context.Usuarios.Remove(usuarioDB);
            _context.SaveChanges();
            return true;
        }       
    }
}
