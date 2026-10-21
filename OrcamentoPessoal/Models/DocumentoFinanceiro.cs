using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace OrcamentoPessoal.Models
{
    public enum StatusProcessamentoIA
    {
        [Display(Name = "Aguardando Leitura")]
        Aguardando = 1,

        [Display(Name = "Processando com IA")]
        EmProcessamento = 2,

        [Display(Name = "Processado com Sucesso")]
        Concluido = 3,

        [Display(Name = "Falha no Processamento")]
        Falha = 4
    }

    public enum FormatoArquivo
    {
        PDF,
        Excel,
        CSV,
        Word,
        Imagem
    }

    public class DocumentoFinanceiro
    {
        public int Id { get; set; }

        [Display(Name = "Nome do Arquivo")]
        public string NomeArquivo { get; set; } = string.Empty;

        [Display(Name = "Formato")]
        public FormatoArquivo Formato { get; set; } = FormatoArquivo.PDF;

        [Display(Name = "Extensão")]
        public string Extensao { get; set; } = ".pdf";

        public long TamanhoBytes { get; set; }

        [Display(Name = "Tamanho")]
        public string TamanhoFormatado { get; set; } = "0 KB";

        [Display(Name = "Data de Envio")]
        public DateTime DataUpload { get; set; } = DateTime.Now;

        [Display(Name = "Status")]
        public StatusProcessamentoIA Status { get; set; } = StatusProcessamentoIA.Aguardando;

        [Display(Name = "Transações Extraídas")]
        public int QtdTransacoesExtraidas { get; set; }

        [Display(Name = "Valor Total Detectado")]
        public decimal ValorTotalDetectado { get; set; }

        [Display(Name = "Acurácia da IA")]
        public int ConfiancaIA { get; set; } = 95;

        [Display(Name = "Resumo da Análise")]
        public string ResumoIA { get; set; } = string.Empty;

        public string ValorDetectadoFormatado => ValorTotalDetectado.ToString("C", new CultureInfo("pt-BR"));
        public string DataFormatada => DataUpload.ToString("dd/MM/yyyy HH:mm");

        public string IconeCss
        {
            get
            {
                switch (Extensao.ToLower())
                {
                    case ".pdf": return "bi-file-earmark-pdf text-danger";
                    case ".xlsx":
                    case ".xls":
                    case ".csv": return "bi-file-earmark-excel text-success";
                    case ".doc":
                    case ".docx": return "bi-file-earmark-word text-primary";
                    default: return "bi-file-earmark-text text-secondary";
                }
            }
        }

        public string BadgeStatusCss
        {
            get
            {
                switch (Status)
                {
                    case StatusProcessamentoIA.Concluido: return "badge bg-success-subtle text-success border border-success-subtle";
                    case StatusProcessamentoIA.EmProcessamento: return "badge bg-info-subtle text-info border border-info-subtle";
                    case StatusProcessamentoIA.Aguardando: return "badge bg-warning-subtle text-warning border border-warning-subtle";
                    default: return "badge bg-danger-subtle text-danger border border-danger-subtle";
                }
            }
        }
    }
}
