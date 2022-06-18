namespace Api.Core.Domain.Localizacoes
{
    public record Endereco
    {
        public string Cep { get; set; }

        public string Cidade { get; set; }

        public string Bairro { get; set; }

        public string Logradouro { get; set; }

        public string Numero { get; set; }
    }
}
