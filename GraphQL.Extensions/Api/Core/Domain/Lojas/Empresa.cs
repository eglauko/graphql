using Api.Core.SeedWork;

namespace Api.Core.Domain.Lojas;

public class Empresa : Entity
{
    public string Nome { get; set; }

    public string RazaoSocial { get; set; }

    public ICollection<Loja> Lojas { get; set; } = new List<Loja>();
}
