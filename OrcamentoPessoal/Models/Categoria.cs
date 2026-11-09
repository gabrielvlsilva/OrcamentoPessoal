using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace OrcamentoPessoal.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome da categoria")]
        [StringLength(60)]
        [Display(Name = "Nome da Categoria")]
        public string Nome { get; set; } = string.Empty;

        public TipoTransacao Tipo { get; set; } = TipoTransacao.Despesa;

        [Display(Name = "Cor")]
        public string CorHex { get; set; } = "#3b82f6";

        [Display(Name = "Ícone")]
        public string IconeCss { get; set; } = "bi-folder";

        [DataType(DataType.Currency)]
        [Display(Name = "Orçamento Previsto")]
        public decimal OrcamentoMensal { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Total Gasto no Mês")]
        public decimal TotalGastoMes { get; set; }

        public int PercentualNoTotalDespesas { get; set; }

        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;

        public int PercentualConsumido
        {
            get
            {
                if (OrcamentoMensal <= 0) return 0;
                var perc = (int)Math.Round((TotalGastoMes / OrcamentoMensal) * 100);
                return Math.Min(perc, 100);
            }
        }

        public string OrcamentoFormatado => OrcamentoMensal.ToString("C", new CultureInfo("pt-BR"));
        public string TotalGastoFormatado => TotalGastoMes.ToString("C", new CultureInfo("pt-BR"));
    }
}
