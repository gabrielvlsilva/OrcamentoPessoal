using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OrcamentoPessoal.Models;

namespace OrcamentoPessoal.Database
{
    public class HomeDB
    {
        public static DashboardViewModel ObterDadosDashboard(Usuario usuario, out string errorMsg)
        {
            errorMsg = string.Empty;
            var dashboard = new DashboardViewModel
            {
                Usuario = usuario ?? new Usuario()
            };

            if (usuario == null || usuario.Id == 0)
            {
                errorMsg = "Não foi possível identificar o usuário atual.";
                return dashboard;
            }

            DateTime hoje = DateTime.Today;
            DateTime inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
            DateTime inicioProximoMes = inicioMes.AddMonths(1);
            dashboard.MesAtualNome = hoje.ToString("MMMM yyyy", new CultureInfo("pt-BR"));
            dashboard.AnoAtual = hoje.Year;

            var parametros = new Dictionary<string, object>
            {
                { "UsuarioId", usuario.Id }
            };

            const string sqlResumo = @"
                SELECT
                    COALESCE(SUM(CASE WHEN TRN_TIP = 1 THEN TRN_VAL WHEN TRN_TIP = 2 THEN -TRN_VAL ELSE 0 END), 0) AS SaldoAtual,
                    COALESCE(SUM(CASE WHEN TRN_TIP = 1 AND TRN_DTA >= @InicioMes AND TRN_DTA < @InicioProximoMes THEN TRN_VAL ELSE 0 END), 0) AS TotalReceitasMes,
                    COALESCE(SUM(CASE WHEN TRN_TIP = 2 AND TRN_DTA >= @InicioMes AND TRN_DTA < @InicioProximoMes THEN TRN_VAL ELSE 0 END), 0) AS TotalDespesasMes
                FROM trn
                WHERE USR_AIC = @UsuarioId
                    AND TRN_STA = @StatusConcluido";

            var resumo = ConexaoBD.ConsultaObjeto<ResumoDashboardBD>(sqlResumo, new Dictionary<string, object>
            {
                { "UsuarioId", usuario.Id },
                { "StatusConcluido", (int)StatusTransacao.Concluido },
                { "InicioMes", inicioMes },
                { "InicioProximoMes", inicioProximoMes }
            }, out errorMsg);

            if (!string.IsNullOrEmpty(errorMsg))
            {
                return dashboard;
            }

            if (resumo != null)
            {
                dashboard.SaldoAtual = resumo.SaldoAtual ?? 0;
                dashboard.TotalReceitasMes = resumo.TotalReceitasMes ?? 0;
                dashboard.TotalDespesasMes = resumo.TotalDespesasMes ?? 0;
            }

            const string sqlCategorias = @"
                SELECT
                    TRN_CAT AS Codigo,
                    SUM(TRN_VAL) AS TotalGastoMes
                FROM trn
                WHERE USR_AIC = @UsuarioId
                    AND TRN_STA = @StatusConcluido
                    AND TRN_TIP = @TipoDespesa
                    AND TRN_DTA >= @InicioMes
                    AND TRN_DTA < @InicioProximoMes
                GROUP BY TRN_CAT
                ORDER BY TotalGastoMes DESC";

            var gastosPorCategoria = ConexaoBD.Consulta<CategoriaResumoBD>(sqlCategorias, new Dictionary<string, object>
            {
                { "UsuarioId", usuario.Id },
                { "StatusConcluido", (int)StatusTransacao.Concluido },
                { "TipoDespesa", (int)TipoTransacao.Despesa },
                { "InicioMes", inicioMes },
                { "InicioProximoMes", inicioProximoMes }
            }, out errorMsg);

            if (!string.IsNullOrEmpty(errorMsg))
            {
                return dashboard;
            }

            var catalogoCategorias = CatalogoCategorias.ObterCategorias();
            foreach (var gasto in gastosPorCategoria)
            {
                var categoriaFixa = catalogoCategorias.FirstOrDefault(item => item.Codigo == gasto.Codigo);
                dashboard.CategoriasResumo.Add(new Categoria
                {
                    Nome = categoriaFixa == null ? "Sem categoria" : categoriaFixa.Nome,
                    Tipo = TipoTransacao.Despesa,
                    TotalGastoMes = gasto.TotalGastoMes,
                    PercentualNoTotalDespesas = dashboard.TotalDespesasMes > 0
                        ? (int)Math.Round(gasto.TotalGastoMes / dashboard.TotalDespesasMes * 100)
                        : 0
                });
            }

            const string sqlTransacoes = @"
                SELECT
                    t.TRN_AIC AS Id,
                    t.TRN_DES AS Descricao,
                    t.TRN_VAL AS Valor,
                    t.TRN_DTA AS DataTransacao,
                    t.TRN_TIP AS Tipo,
                    t.TRN_CAT AS CategoriaCodigo,
                    t.TRN_FPG AS FormaPagamento,
                    t.TRN_STA AS Status,
                    t.TRN_CNF AS ConfiancaIA,
                    t.TRN_PAG AS PaginaOrigem,
                    t.TRN_PLA AS PlanilhaOrigem,
                    t.TRN_LIN AS LinhaOrigem,
                    t.DOC_AIC AS DocumentoOrigemId,
                    COALESCE(d.DOC_NOM, '') AS NomeArquivoOrigem
                FROM trn t
                LEFT JOIN doc d ON d.DOC_AIC = t.DOC_AIC
                WHERE t.USR_AIC = @UsuarioId
                    AND t.TRN_STA = @StatusConcluido
                ORDER BY COALESCE(t.TRN_DTA, DATE(t.TRN_DTC)) DESC, t.TRN_AIC DESC
                LIMIT 10";

            dashboard.UltimasTransacoes = ConexaoBD.Consulta<Transacao>(sqlTransacoes, new Dictionary<string, object>
            {
                { "UsuarioId", usuario.Id },
                { "StatusConcluido", (int)StatusTransacao.Concluido }
            }, out errorMsg);

            if (!string.IsNullOrEmpty(errorMsg))
            {
                return dashboard;
            }

            foreach (var transacao in dashboard.UltimasTransacoes)
            {
                var categoria = CatalogoCategorias.ObterCategorias().FirstOrDefault(item => item.Codigo == transacao.CategoriaCodigo);
                if (categoria != null)
                {
                    transacao.CategoriaNome = categoria.Nome;
                }
            }

            const string sqlEstatisticas = @"
                SELECT
                    COUNT(DISTINCT CASE WHEN d.DOC_STA IN ('Importado', 'Analisado') THEN d.DOC_AIC END) AS TotalDocumentosProcessados,
                    COUNT(t.TRN_AIC) AS TotalTransacoesExtraidasIA
                FROM doc d
                LEFT JOIN trn t ON t.DOC_AIC = d.DOC_AIC
                WHERE d.USR_AIC = @UsuarioId";

            var estatisticas = ConexaoBD.ConsultaObjeto<EstatisticasDashboardBD>(sqlEstatisticas, parametros, out errorMsg);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                return dashboard;
            }

            if (estatisticas != null)
            {
                dashboard.TotalDocumentosProcessados = estatisticas.TotalDocumentosProcessados;
                dashboard.TotalTransacoesExtraidasIA = estatisticas.TotalTransacoesExtraidasIA;
            }

            const string sqlDocumentos = @"
                SELECT
                    d.DOC_AIC AS Id,
                    d.DOC_NOM AS NomeArquivo,
                    d.DOC_EXT AS Extensao,
                    d.DOC_TAM AS TamanhoBytes,
                    d.DOC_DTU AS DataUpload,
                    CASE d.DOC_EXT
                        WHEN '.xlsx' THEN 1
                        WHEN '.csv' THEN 2
                        ELSE 0
                    END AS Formato,
                    CASE
                        WHEN d.DOC_STA IN ('Importado', 'Analisado') THEN 3
                        WHEN d.DOC_STA LIKE 'Falha%' THEN 4
                        WHEN d.DOC_STA = 'Recebido' THEN 1
                        ELSE 2
                    END AS Status,
                    COUNT(t.TRN_AIC) AS QtdTransacoesExtraidas,
                    COALESCE(SUM(t.TRN_VAL), 0) AS ValorTotalDetectado,
                    COALESCE(ROUND(AVG(t.TRN_CNF) * 100), 0) AS ConfiancaIA,
                    CASE
                        WHEN COUNT(t.TRN_AIC) = 0 THEN 'Nenhum lançamento identificado'
                        ELSE CONCAT(COUNT(t.TRN_AIC), ' lançamentos importados')
                    END AS ResumoIA
                FROM doc d
                LEFT JOIN trn t ON t.DOC_AIC = d.DOC_AIC
                WHERE d.USR_AIC = @UsuarioId
                GROUP BY d.DOC_AIC, d.DOC_NOM, d.DOC_EXT, d.DOC_TAM, d.DOC_DTU, d.DOC_STA
                ORDER BY d.DOC_DTU DESC
                LIMIT 5";

            dashboard.DocumentosRecentes = ConexaoBD.Consulta<DocumentoFinanceiro>(sqlDocumentos, parametros, out errorMsg);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                return dashboard;
            }

            foreach (var documento in dashboard.DocumentosRecentes)
            {
                documento.TamanhoFormatado = FormatarTamanho(documento.TamanhoBytes);
            }

            return dashboard;
        }

        private static string FormatarTamanho(long tamanhoBytes)
        {
            if (tamanhoBytes >= 1024 * 1024)
            {
                return (tamanhoBytes / (1024d * 1024d)).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
            }

            return Math.Max(1, (int)Math.Ceiling(tamanhoBytes / 1024d)) + " KB";
        }
    }

    public class ResumoDashboardBD
    {
        public decimal? SaldoAtual { get; set; }
        public decimal? TotalReceitasMes { get; set; }
        public decimal? TotalDespesasMes { get; set; }
    }

    public class EstatisticasDashboardBD
    {
        public int TotalDocumentosProcessados { get; set; }
        public int TotalTransacoesExtraidasIA { get; set; }
    }

    public class CategoriaResumoBD
    {
        public string Codigo { get; set; }
        public decimal TotalGastoMes { get; set; }
    }
}
