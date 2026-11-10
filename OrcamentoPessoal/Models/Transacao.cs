using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace OrcamentoPessoal.Models
{
    public enum TipoTransacao
    {
        [Display(Name = "Receita")]
        Receita = 1,

        [Display(Name = "Despesa")]
        Despesa = 2
    }

    public enum StatusTransacao
    {
        [Display(Name = "Pendente")]
        Pendente = 1,

        [Display(Name = "Concluído")]
        Concluido = 2,

        [Display(Name = "Cancelado")]
        Cancelado = 3
    }

    public class Transacao
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe a descrição do lançamento")]
        [Display(Name = "Descrição")]
        [StringLength(150, ErrorMessage = "A descrição deve ter no máximo 150 caracteres")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o valor")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        [DataType(DataType.Currency)]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "Informe a data")]
        [DataType(DataType.Date)]
        [Display(Name = "Data da Transação")]
        public DateTime? DataTransacao { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Data de Vencimento")]
        public DateTime? DataVencimento { get; set; }

        [Required(ErrorMessage = "Selecione o tipo")]
        public TipoTransacao Tipo { get; set; } = TipoTransacao.Despesa;

        [Display(Name = "Categoria")]
        public string CategoriaNome { get; set; } = "Geral";

        public string CategoriaCodigo { get; set; } = string.Empty;

        public string CategoriaIcone { get; set; } = "bi-tag";

        public string CategoriaCor { get; set; } = "#6366f1";

        [Display(Name = "Forma de Pagamento")]
        public string FormaPagamento { get; set; } = "Pix";

        public StatusTransacao Status { get; set; } = StatusTransacao.Concluido;

        [Display(Name = "Observação")]
        public string Observacao { get; set; } = string.Empty;
        public int? DocumentoOrigemId { get; set; }

        [Display(Name = "Arquivo de Origem")]
        public string NomeArquivoOrigem { get; set; } = string.Empty;

        public decimal ConfiancaIA { get; set; }

        public int? PaginaOrigem { get; set; }

        public string PlanilhaOrigem { get; set; } = string.Empty;

        public int? LinhaOrigem { get; set; }

        public bool CriadoPorIA => DocumentoOrigemId.HasValue;

        public string ValorFormatado => Valor.ToString("C", new CultureInfo("pt-BR"));
        public string DataFormatada => DataTransacao.HasValue ? DataTransacao.Value.ToString("dd/MM/yyyy") : "—";
    }
}
