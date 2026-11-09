using System;
using Newtonsoft.Json;

namespace OrcamentoPessoal.Models
{
    public class TransacaoSugeridaViewModel
    {
        [JsonProperty("date")]
        public DateTime? Data { get; set; }

        [JsonProperty("description")]
        public string Descricao { get; set; }

        [JsonProperty("amount")]
        public decimal Valor { get; set; }

        [JsonProperty("type")]
        public string Tipo { get; set; }

        [JsonProperty("categoryCode")]
        public string CategoriaCodigo { get; set; }

        [JsonProperty("categoryName")]
        public string CategoriaNome { get; set; }

        [JsonProperty("paymentMethod")]
        public string FormaPagamento { get; set; }

        [JsonProperty("source")]
        public OrigemTransacaoViewModel Origem { get; set; }

        [JsonProperty("confidence")]
        public double Confianca { get; set; }
    }

    public class OrigemTransacaoViewModel
    {
        [JsonProperty("pageNumber")]
        public int? Pagina { get; set; }

        [JsonProperty("sheetName")]
        public string Planilha { get; set; }

        [JsonProperty("rowNumber")]
        public int? Linha { get; set; }
    }
}
