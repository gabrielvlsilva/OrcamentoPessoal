using System;
using System.Collections.Generic;
using System.Globalization;

namespace OrcamentoPessoal.Models
{
    public class RelatoriosViewModel
    {
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");

        public int MesReferencia { get; set; }
        public int AnoReferencia { get; set; }
        public string PeriodoNome { get; set; } = string.Empty;
        public string Erro { get; set; } = string.Empty;
        public string Manchete { get; set; } = string.Empty;
        public bool MesAtual { get; set; }
        public int DiasNoMes { get; set; }
        public int DiasDecorridos { get; set; }
        public bool TemHistorico { get; set; }

        public decimal TotalReceitas { get; set; }
        public decimal TotalDespesas { get; set; }
        public decimal ReceitasMesAnterior { get; set; }
        public decimal DespesasMesAnterior { get; set; }
        public decimal SaldoPeriodo => TotalReceitas - TotalDespesas;
        public decimal SaldoAcumulado { get; set; }
        public int QuantidadeReceitas { get; set; }
        public int QuantidadeDespesas { get; set; }
        public int TotalLancamentos => QuantidadeReceitas + QuantidadeDespesas;
        public int TotalDocumentos { get; set; }

        public decimal? TaxaPoupanca => TotalReceitas > 0 ? Math.Round(SaldoPeriodo / TotalReceitas * 100m, 1) : (decimal?)null;
        public decimal? VariacaoReceitas => Variacao(TotalReceitas, ReceitasMesAnterior);
        public decimal? VariacaoDespesas => Variacao(TotalDespesas, DespesasMesAnterior);
        public decimal MediaDiariaDespesas { get; set; }
        public decimal MediaDiariaMesAnterior { get; set; }
        public decimal? ProjecaoDespesas { get; set; }

        public int? ScoreFinanceiro { get; set; }
        public string ScoreRotulo { get; set; } = string.Empty;
        public string ScoreCor { get; set; } = "#94a3b8";
        public List<ScoreItemViewModel> ScoreItens { get; set; } = new List<ScoreItemViewModel>();

        public List<InsightViewModel> Insights { get; set; } = new List<InsightViewModel>();

        public List<RelatorioCategoriaViewModel> DespesasPorCategoria { get; set; } = new List<RelatorioCategoriaViewModel>();
        public List<RelatorioCategoriaViewModel> ReceitasPorCategoria { get; set; } = new List<RelatorioCategoriaViewModel>();
        public List<RelatorioCategoriaViewModel> ComparativoDespesas { get; set; } = new List<RelatorioCategoriaViewModel>();

        public List<RelatorioMesViewModel> EvolucaoMensal { get; set; } = new List<RelatorioMesViewModel>();
        public List<RelatorioDiaViewModel> Dias { get; set; } = new List<RelatorioDiaViewModel>();
        public List<decimal?> AcumuladoDespesas { get; set; } = new List<decimal?>();
        public List<decimal?> AcumuladoDespesasAnterior { get; set; } = new List<decimal?>();
        public List<decimal> GastoMedioPorDiaSemana { get; set; } = new List<decimal>();
        public string DiaSemanaMaisCaro { get; set; } = string.Empty;

        public List<RelatorioRankingViewModel> TopEstabelecimentos { get; set; } = new List<RelatorioRankingViewModel>();
        public List<Transacao> MaioresDespesas { get; set; } = new List<Transacao>();

        public int ConfiancaMediaIA { get; set; }
        public int LancamentosIA { get; set; }
        public int LancamentosBaixaConfianca { get; set; }
        public int LancamentosSemData { get; set; }

        public List<Transacao> LancamentosRecentes { get; set; } = new List<Transacao>();

        public string ReceitasFormatado => TotalReceitas.ToString("C", PtBr);
        public string DespesasFormatado => TotalDespesas.ToString("C", PtBr);
        public string SaldoFormatado => SaldoPeriodo.ToString("C", PtBr);
        public string SaldoAcumuladoFormatado => SaldoAcumulado.ToString("C", PtBr);
        public string ReceitasMesAnteriorFormatado => ReceitasMesAnterior.ToString("C", PtBr);
        public string DespesasMesAnteriorFormatado => DespesasMesAnterior.ToString("C", PtBr);
        public string MediaDiariaFormatada => MediaDiariaDespesas.ToString("C", PtBr);
        public string MediaDiariaAnteriorFormatada => MediaDiariaMesAnterior.ToString("C", PtBr);
        public string ProjecaoFormatada => ProjecaoDespesas.HasValue ? ProjecaoDespesas.Value.ToString("C", PtBr) : "—";

        public static decimal? Variacao(decimal atual, decimal anterior)
        {
            return anterior > 0 ? Math.Round((atual - anterior) / anterior * 100m, 1) : (decimal?)null;
        }
    }

    public class ScoreItemViewModel
    {
        public string Nome { get; set; } = string.Empty;
        public string Dica { get; set; } = string.Empty;
        public int Pontos { get; set; }
        public int Maximo { get; set; }
        public int Percentual => Maximo > 0 ? (int)Math.Round(Pontos * 100d / Maximo) : 0;
    }

    public class InsightViewModel
    {
        public string Nivel { get; set; } = "info";
        public string Titulo { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;

        public string Icone
        {
            get
            {
                switch (Nivel)
                {
                    case "success": return "bi-check-circle-fill";
                    case "warning": return "bi-exclamation-triangle-fill";
                    case "danger": return "bi-x-octagon-fill";
                    default: return "bi-lightbulb-fill";
                }
            }
        }
    }

    public class RelatorioCategoriaViewModel
    {
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");

        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Cor { get; set; } = "#94a3b8";
        public decimal Valor { get; set; }
        public decimal ValorAnterior { get; set; }
        public int Quantidade { get; set; }
        public int Percentual { get; set; }
        public decimal? Variacao => RelatoriosViewModel.Variacao(Valor, ValorAnterior);
        public string ValorFormatado => Valor.ToString("C", PtBr);
        public string ValorAnteriorFormatado => ValorAnterior.ToString("C", PtBr);
    }

    public class RelatorioMesViewModel
    {
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");

        public int Mes { get; set; }
        public int Ano { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Receitas { get; set; }
        public decimal Despesas { get; set; }
        public int PercentualReceitas { get; set; }
        public int PercentualDespesas { get; set; }
        public decimal SaldoAcumulado { get; set; }
        public decimal Saldo => Receitas - Despesas;
        public decimal? TaxaPoupanca => Receitas > 0 ? Math.Round(Saldo / Receitas * 100m, 1) : (decimal?)null;
        public string Rotulo => Nome.TrimEnd('.') + "/" + (Ano % 100).ToString("00");
        public string ReceitasFormatado => Receitas.ToString("C", PtBr);
        public string DespesasFormatado => Despesas.ToString("C", PtBr);
    }

    public class RelatorioDiaViewModel
    {
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");

        public int Dia { get; set; }
        public DateTime Data { get; set; }
        public decimal Receitas { get; set; }
        public decimal Despesas { get; set; }
        public int Quantidade { get; set; }
        public int Nivel { get; set; }
        public string DespesasFormatado => Despesas.ToString("C", PtBr);
        public string ReceitasFormatado => Receitas.ToString("C", PtBr);
    }

    public class RelatorioRankingViewModel
    {
        private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");

        public string Nome { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public int Quantidade { get; set; }
        public int Percentual { get; set; }
        public string ValorFormatado => Valor.ToString("C", PtBr);
    }
}
