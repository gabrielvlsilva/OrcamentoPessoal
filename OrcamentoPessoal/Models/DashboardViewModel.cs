using System;
using System.Collections.Generic;
using System.Globalization;

namespace OrcamentoPessoal.Models
{
    public class DashboardViewModel
    {
        public Usuario Usuario { get; set; } = new Usuario();

        public decimal SaldoAtual { get; set; }
        public decimal TotalReceitasMes { get; set; }
        public decimal TotalDespesasMes { get; set; }
        public decimal EconomiaPrevistaMes { get; set; }

        public int TotalDocumentosProcessados { get; set; }
        public int TotalTransacoesExtraidasIA { get; set; }

        public List<Transacao> UltimasTransacoes { get; set; } = new List<Transacao>();
        public List<DocumentoFinanceiro> DocumentosRecentes { get; set; } = new List<DocumentoFinanceiro>();
        public List<Categoria> CategoriasResumo { get; set; } = new List<Categoria>();
        public string MesAtualNome { get; set; } = DateTime.Now.ToString("MMMM yyyy", new CultureInfo("pt-BR"));
        public int AnoAtual { get; set; } = DateTime.Now.Year;

        public string SaldoFormatado => SaldoAtual.ToString("C", new CultureInfo("pt-BR"));
        public string ReceitasFormatado => TotalReceitasMes.ToString("C", new CultureInfo("pt-BR"));
        public string DespesasFormatado => TotalDespesasMes.ToString("C", new CultureInfo("pt-BR"));
        public string EconomiaFormatada => EconomiaPrevistaMes.ToString("C", new CultureInfo("pt-BR"));
    }
}
