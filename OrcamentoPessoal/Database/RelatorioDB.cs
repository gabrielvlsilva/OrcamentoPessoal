using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OrcamentoPessoal.Models;

namespace OrcamentoPessoal.Database
{
    public class RelatorioDB
    {
        public static RelatoriosViewModel Obter(uint usuarioId, int ano, int mes, out string errorMsg)
        {
            errorMsg = string.Empty;
            if (usuarioId == 0 || mes < 1 || mes > 12 || ano < 2000 || ano > 2100)
            {
                errorMsg = "O período informado para o relatório é inválido.";
                return new RelatoriosViewModel { AnoReferencia = ano, MesReferencia = mes };
            }

            var relatorio = new RelatoriosViewModel
            {
                AnoReferencia = ano,
                MesReferencia = mes,
                PeriodoNome = new DateTime(ano, mes, 1).ToString("MMMM yyyy", new CultureInfo("pt-BR"))
            };

            DateTime inicioMes = new DateTime(ano, mes, 1);
            DateTime inicioProximoMes = inicioMes.AddMonths(1);
            DateTime inicioMesAnterior = inicioMes.AddMonths(-1);
            var parametrosPeriodo = new Dictionary<string, object>
            {
                { "UsuarioId", usuarioId },
                { "StatusConcluido", (int)StatusTransacao.Concluido },
                { "InicioMes", inicioMes },
                { "InicioProximoMes", inicioProximoMes },
                { "InicioMesAnterior", inicioMesAnterior }
            };

            const string sqlResumo = @"
                SELECT
                    COALESCE(SUM(CASE WHEN TRN_DTA >= @InicioMes AND TRN_DTA < @InicioProximoMes AND TRN_TIP = 1 THEN TRN_VAL ELSE 0 END), 0) AS TotalReceitas,
                    COALESCE(SUM(CASE WHEN TRN_DTA >= @InicioMes AND TRN_DTA < @InicioProximoMes AND TRN_TIP = 2 THEN TRN_VAL ELSE 0 END), 0) AS TotalDespesas,
                    COALESCE(SUM(CASE WHEN TRN_DTA >= @InicioMesAnterior AND TRN_DTA < @InicioMes AND TRN_TIP = 1 THEN TRN_VAL ELSE 0 END), 0) AS ReceitasMesAnterior,
                    COALESCE(SUM(CASE WHEN TRN_DTA >= @InicioMesAnterior AND TRN_DTA < @InicioMes AND TRN_TIP = 2 THEN TRN_VAL ELSE 0 END), 0) AS DespesasMesAnterior,
                    COUNT(CASE WHEN TRN_DTA >= @InicioMes AND TRN_DTA < @InicioProximoMes AND TRN_TIP = 1 THEN 1 END) AS QuantidadeReceitas,
                    COUNT(CASE WHEN TRN_DTA >= @InicioMes AND TRN_DTA < @InicioProximoMes AND TRN_TIP = 2 THEN 1 END) AS QuantidadeDespesas
                FROM trn
                WHERE USR_AIC = @UsuarioId
                    AND TRN_STA = @StatusConcluido
                    AND TRN_DTA >= @InicioMesAnterior
                    AND TRN_DTA < @InicioProximoMes";

            var resumo = ConexaoBD.ConsultaObjeto<ResumoRelatorioBD>(sqlResumo, parametrosPeriodo, out errorMsg);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                return relatorio;
            }

            if (resumo != null)
            {
                relatorio.TotalReceitas = resumo.TotalReceitas;
                relatorio.TotalDespesas = resumo.TotalDespesas;
                relatorio.ReceitasMesAnterior = resumo.ReceitasMesAnterior;
                relatorio.DespesasMesAnterior = resumo.DespesasMesAnterior;
                relatorio.QuantidadeReceitas = resumo.QuantidadeReceitas;
                relatorio.QuantidadeDespesas = resumo.QuantidadeDespesas;
            }

            const string sqlCategorias = @"
                SELECT TRN_CAT AS Codigo, TRN_TIP AS Tipo, SUM(TRN_VAL) AS Valor, COUNT(*) AS Quantidade
                FROM trn
                WHERE USR_AIC = @UsuarioId
                    AND TRN_STA = @StatusConcluido
                    AND TRN_DTA >= @InicioMes
                    AND TRN_DTA < @InicioProximoMes
                GROUP BY TRN_CAT, TRN_TIP
                ORDER BY TRN_TIP, Valor DESC";

            var categorias = ConexaoBD.Consulta<RelatorioCategoriaBD>(sqlCategorias, new Dictionary<string, object>
            {
                { "UsuarioId", usuarioId },
                { "StatusConcluido", (int)StatusTransacao.Concluido },
                { "InicioMes", inicioMes },
                { "InicioProximoMes", inicioProximoMes }
            }, out errorMsg);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                return relatorio;
            }

            var catalogo = CatalogoCategorias.ObterCategorias();
            foreach (var categoria in categorias)
            {
                var categoriaFixa = catalogo.FirstOrDefault(categoriaCatalogo => categoriaCatalogo.Codigo == categoria.Codigo);
                var item = new RelatorioCategoriaViewModel
                {
                    Codigo = categoria.Codigo,
                    Nome = categoriaFixa == null ? "Sem categoria" : categoriaFixa.Nome,
                    Valor = categoria.Valor,
                    Quantidade = categoria.Quantidade
                };

                if (categoria.Tipo == (int)TipoTransacao.Despesa)
                {
                    relatorio.DespesasPorCategoria.Add(item);
                }
                else if (categoria.Tipo == (int)TipoTransacao.Receita)
                {
                    relatorio.ReceitasPorCategoria.Add(item);
                }
            }

            CalcularPercentuais(relatorio.DespesasPorCategoria, relatorio.TotalDespesas);
            CalcularPercentuais(relatorio.ReceitasPorCategoria, relatorio.TotalReceitas);

            DateTime inicioEvolucao = inicioMes.AddMonths(-5);
            const string sqlEvolucao = @"
                SELECT YEAR(TRN_DTA) AS Ano, MONTH(TRN_DTA) AS Mes,
                    SUM(CASE WHEN TRN_TIP = 1 THEN TRN_VAL ELSE 0 END) AS Receitas,
                    SUM(CASE WHEN TRN_TIP = 2 THEN TRN_VAL ELSE 0 END) AS Despesas
                FROM trn
                WHERE USR_AIC = @UsuarioId
                    AND TRN_STA = @StatusConcluido
                    AND TRN_DTA >= @InicioEvolucao
                    AND TRN_DTA < @InicioProximoMes
                GROUP BY YEAR(TRN_DTA), MONTH(TRN_DTA)
                ORDER BY Ano, Mes";

            var valoresMensais = ConexaoBD.Consulta<RelatorioMesBD>(sqlEvolucao, new Dictionary<string, object>
            {
                { "UsuarioId", usuarioId },
                { "StatusConcluido", (int)StatusTransacao.Concluido },
                { "InicioEvolucao", inicioEvolucao },
                { "InicioProximoMes", inicioProximoMes }
            }, out errorMsg);

            if (!string.IsNullOrEmpty(errorMsg))
            {
                return relatorio;
            }

            for (int indice = 0; indice < 6; indice++)
            {
                DateTime dataMes = inicioEvolucao.AddMonths(indice);
                var valorMes = valoresMensais.FirstOrDefault(item => item.Ano == dataMes.Year && item.Mes == dataMes.Month);
                relatorio.EvolucaoMensal.Add(new RelatorioMesViewModel
                {
                    Ano = dataMes.Year,
                    Mes = dataMes.Month,
                    Nome = dataMes.ToString("MMM", new CultureInfo("pt-BR")),
                    Receitas = valorMes == null ? 0 : valorMes.Receitas,
                    Despesas = valorMes == null ? 0 : valorMes.Despesas
                });
            }

            decimal maiorValorMensal = relatorio.EvolucaoMensal
                .Select(item => Math.Max(item.Receitas, item.Despesas))
                .DefaultIfEmpty(0)
                .Max();
            foreach (var item in relatorio.EvolucaoMensal)
            {
                item.PercentualReceitas = maiorValorMensal > 0 ? (int)Math.Round(item.Receitas / maiorValorMensal * 100) : 0;
                item.PercentualDespesas = maiorValorMensal > 0 ? (int)Math.Round(item.Despesas / maiorValorMensal * 100) : 0;
            }

            const string sqlDocumentos = @"
                SELECT COUNT(*) AS Total
                FROM doc
                WHERE USR_AIC = @UsuarioId
                    AND DOC_STA IN ('Importado', 'Analisado')
                    AND DOC_DTU >= @InicioMes
                    AND DOC_DTU < @InicioProximoMes";

            var documentos = ConexaoBD.ConsultaObjeto<ContagemRelatorioBD>(sqlDocumentos, new Dictionary<string, object>
            {
                { "UsuarioId", usuarioId },
                { "InicioMes", inicioMes },
                { "InicioProximoMes", inicioProximoMes }
            }, out errorMsg);

            if (!string.IsNullOrEmpty(errorMsg))
            {
                return relatorio;
            }

            relatorio.TotalDocumentos = documentos == null ? 0 : documentos.Total;
            relatorio.LancamentosRecentes = ObterLancamentos(usuarioId, inicioMes, inicioProximoMes, 20, out errorMsg);
            return relatorio;
        }

        public static List<Transacao> ObterLancamentos(uint usuarioId, DateTime inicio, DateTime fim, int? limite, out string errorMsg)
        {
            string sql = @"
                SELECT
                    t.TRN_AIC AS Id,
                    t.TRN_DES AS Descricao,
                    t.TRN_VAL AS Valor,
                    t.TRN_DTA AS DataTransacao,
                    t.TRN_TIP AS Tipo,
                    t.TRN_CAT AS CategoriaCodigo,
                    t.TRN_FPG AS FormaPagamento,
                    t.TRN_STA AS Status,
                    t.DOC_AIC AS DocumentoOrigemId,
                    COALESCE(d.DOC_NOM, '') AS NomeArquivoOrigem
                FROM trn t
                LEFT JOIN doc d ON d.DOC_AIC = t.DOC_AIC
                WHERE t.USR_AIC = @UsuarioId
                    AND t.TRN_STA = @StatusConcluido
                    AND t.TRN_DTA >= @Inicio
                    AND t.TRN_DTA < @Fim
                ORDER BY t.TRN_DTA DESC, t.TRN_AIC DESC";

            if (limite.HasValue)
            {
                sql += " LIMIT " + Math.Max(1, Math.Min(limite.Value, 100)).ToString(CultureInfo.InvariantCulture);
            }

            var transacoes = ConexaoBD.Consulta<Transacao>(sql, new Dictionary<string, object>
            {
                { "UsuarioId", usuarioId },
                { "StatusConcluido", (int)StatusTransacao.Concluido },
                { "Inicio", inicio },
                { "Fim", fim }
            }, out errorMsg);

            if (!string.IsNullOrEmpty(errorMsg) || transacoes == null)
            {
                return transacoes;
            }

            var catalogo = CatalogoCategorias.ObterCategorias();
            foreach (var transacao in transacoes)
            {
                var categoria = catalogo.FirstOrDefault(item => item.Codigo == transacao.CategoriaCodigo);
                if (categoria != null)
                {
                    transacao.CategoriaNome = categoria.Nome;
                }
                else
                {
                    transacao.CategoriaNome = "Sem categoria";
                }
            }

            return transacoes;
        }

        private static void CalcularPercentuais(List<RelatorioCategoriaViewModel> categorias, decimal total)
        {
            foreach (var categoria in categorias)
            {
                categoria.Percentual = total > 0 ? (int)Math.Round(categoria.Valor / total * 100) : 0;
            }
        }
    }

    public class ResumoRelatorioBD
    {
        public decimal TotalReceitas { get; set; }
        public decimal TotalDespesas { get; set; }
        public decimal ReceitasMesAnterior { get; set; }
        public decimal DespesasMesAnterior { get; set; }
        public int QuantidadeReceitas { get; set; }
        public int QuantidadeDespesas { get; set; }
    }

    public class RelatorioCategoriaBD
    {
        public string Codigo { get; set; }
        public int Tipo { get; set; }
        public decimal Valor { get; set; }
        public int Quantidade { get; set; }
    }

    public class RelatorioMesBD
    {
        public int Ano { get; set; }
        public int Mes { get; set; }
        public decimal Receitas { get; set; }
        public decimal Despesas { get; set; }
    }

    public class ContagemRelatorioBD
    {
        public int Total { get; set; }
    }
}
