using System.Collections.Generic;

namespace OrcamentoPessoal.Models
{
    public class CategoriaFixa
    {
        public string Codigo { get; set; }
        public string Nome { get; set; }
        public string Tipo { get; set; }
    }

    public static class CatalogoCategorias
    {
        public static List<CategoriaFixa> ObterCategorias()
        {
            return new List<CategoriaFixa>
            {
                new CategoriaFixa { Codigo = "DESPESA_MORADIA", Nome = "Moradia", Tipo = "Despesa" },
                new CategoriaFixa { Codigo = "DESPESA_ALIMENTACAO", Nome = "Alimentação", Tipo = "Despesa" },
                new CategoriaFixa { Codigo = "DESPESA_TRANSPORTE", Nome = "Transporte", Tipo = "Despesa" },
                new CategoriaFixa { Codigo = "DESPESA_SAUDE", Nome = "Saúde", Tipo = "Despesa" },
                new CategoriaFixa { Codigo = "DESPESA_EDUCACAO", Nome = "Educação", Tipo = "Despesa" },
                new CategoriaFixa { Codigo = "DESPESA_LAZER", Nome = "Lazer", Tipo = "Despesa" },
                new CategoriaFixa { Codigo = "DESPESA_COMPRAS", Nome = "Compras", Tipo = "Despesa" },
                new CategoriaFixa { Codigo = "DESPESA_CONTAS_SERVICOS", Nome = "Contas e Serviços", Tipo = "Despesa" },
                new CategoriaFixa { Codigo = "DESPESA_IMPOSTOS_TAXAS", Nome = "Impostos e Taxas", Tipo = "Despesa" },
                new CategoriaFixa { Codigo = "DESPESA_OUTROS", Nome = "Outros", Tipo = "Despesa" },
                new CategoriaFixa { Codigo = "RECEITA_SALARIO", Nome = "Salário", Tipo = "Receita" },
                new CategoriaFixa { Codigo = "RECEITA_RENDA_EXTRA", Nome = "Renda Extra", Tipo = "Receita" },
                new CategoriaFixa { Codigo = "RECEITA_INVESTIMENTOS", Nome = "Investimentos", Tipo = "Receita" },
                new CategoriaFixa { Codigo = "RECEITA_REEMBOLSOS", Nome = "Reembolsos", Tipo = "Receita" },
                new CategoriaFixa { Codigo = "RECEITA_OUTROS", Nome = "Outros Rendimentos", Tipo = "Receita" }
            };
        }
    }
}
