using System.Collections.Generic;

namespace OrcamentoPessoal.Models
{
    public class AnaliseArquivoViewModel
    {
        public string NomeArquivo { get; set; } = string.Empty;
        public string Formato { get; set; } = string.Empty;
        public List<TransacaoSugeridaViewModel> Transacoes { get; set; } = new List<TransacaoSugeridaViewModel>();
        public List<string> Avisos { get; set; } = new List<string>();
        public bool ResultadoTruncado { get; set; }
        public bool PDFSemTextoExtraivel { get; set; }
    }
}
