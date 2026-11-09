using System;
using System.Collections.Generic;
using System.Globalization;

namespace OrcamentoPessoal.Models
{
    public class RelatoriosViewModel
    {
        public int MesReferencia { get; set; }
        public int AnoReferencia { get; set; }
        public string PeriodoNome { get; set; } = string.Empty;
        public string Erro { get; set; } = string.Empty;

        public decimal TotalReceitas { get; set; }
        public decimal TotalDespesas { get; set; }
        public decimal ReceitasMesAnterior { get; set; }
        public decimal DespesasMesAnterior { get; set; }
        public decimal SaldoPeriodo => TotalReceitas - TotalDespesas;
        public int QuantidadeReceitas { get; set; }
        public int QuantidadeDespesas { get; set; }
        public int TotalDocumentos { get; set; }
        public int TotalLancamentos => QuantidadeReceitas + QuantidadeDespesas;

        public List<RelatorioCategoriaViewModel> DespesasPorCategoria { get; set; } = new List<RelatorioCategoriaViewModel>();
        public List<RelatorioCategoriaViewModel> ReceitasPorCategoria { get; set; } = new List<RelatorioCategoriaViewModel>();
        public List<RelatorioMesViewModel> EvolucaoMensal { get; set; } = new List<RelatorioMesViewModel>();
        public List<Transacao> LancamentosRecentes { get; set; } = new List<Transacao>();

        public string ReceitasFormatado => TotalReceitas.ToString("C", new CultureInfo("pt-BR"));
        public string DespesasFormatado => TotalDespesas.ToString("C", new CultureInfo("pt-BR"));
        public string SaldoFormatado => SaldoPeriodo.ToString("C", new CultureInfo("pt-BR"));
        public string ReceitasMesAnteriorFormatado => ReceitasMesAnterior.ToString("C", new CultureInfo("pt-BR"));
        public string DespesasMesAnteriorFormatado => DespesasMesAnterior.ToString("C", new CultureInfo("pt-BR"));
    }

    public class RelatorioCategoriaViewModel
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public int Quantidade { get; set; }
        public int Percentual { get; set; }
        public string ValorFormatado => Valor.ToString("C", new CultureInfo("pt-BR"));
    }

    public class RelatorioMesViewModel
    {
        public int Mes { get; set; }
        public int Ano { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Receitas { get; set; }
        public decimal Despesas { get; set; }
        public int PercentualReceitas { get; set; }
        public int PercentualDespesas { get; set; }
        public string ReceitasFormatado => Receitas.ToString("C", new CultureInfo("pt-BR"));
        public string DespesasFormatado => Despesas.ToString("C", new CultureInfo("pt-BR"));
    }
}
