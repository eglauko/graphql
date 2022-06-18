using Api.Core.SeedWork;

namespace Api.Core.Domain.Lojas
{
    public class Loja : Entity
    {
        public string Nome { get; set; }

        public string Cnpj { get; set; }

        public string Endereco { get; set; }

        public string Telefone { get; set; }
    }
}
