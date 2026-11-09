using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OrcamentoPessoal.Models
{
    public class RevisaoTransacoesViewModel
    {
        public List<Transacao> Transacoes { get; set; } = new List<Transacao>();
        public List<CategoriaFixa> Categorias { get; set; } = new List<CategoriaFixa>();
    }

    public class EditarTransacaoImportadaViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "O lançamento informado é inválido.")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe a descrição.")]
        [StringLength(150, ErrorMessage = "A descrição deve ter no máximo 150 caracteres.")]
        public string Descricao { get; set; }

        [Required(ErrorMessage = "Informe o valor.")]
        public string Valor { get; set; }

        public DateTime? DataTransacao { get; set; }

        [Required(ErrorMessage = "Selecione uma categoria.")]
        public string CategoriaCodigo { get; set; }
    }
}
