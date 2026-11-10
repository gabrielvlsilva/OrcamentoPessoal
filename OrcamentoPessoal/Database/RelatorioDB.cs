using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OrcamentoPessoal.Models;

namespace OrcamentoPessoal.Database
{
    public class RelatorioDB
    {
        private static readonly string[] CoresCategorias =
        {
            "#6366f1", "#06b6d4", "#10b981", "#f59e0b", "#f43f5e",
            "#8b5cf6", "#14b8a6", "#3b82f6", "#ec4899", "#64748b"
        };

        public static DateTime? ObterUltimoPeriodo(uint usuarioId, out string errorMsg)
        {
            const string sql = @"
                SELECT YEAR(TRN_DTA) AS Ano, MONTH(TRN_DTA) AS Mes
                FROM trn
                WHERE USR_AIC = @UsuarioId
                    AND TRN_STA = @StatusConcluido
                    AND TRN_DTA IS NOT NULL
                ORDER BY TRN_DTA DESC, TRN_AIC DESC
                LIMIT 1";

            var periodo = ConexaoBD.ConsultaObjeto<PeriodoRelatorioBD>(sql, new Dictionary<string, object>
            {
                { "UsuarioId", usuarioId },
                { "StatusConcluido", (int)StatusTransacao.Concluido }
            }, out errorMsg);

            return periodo == null ? (DateTime?)null : new DateTime(periodo.Ano, periodo.Mes, 1);
        }

        public static RelatoriosViewModel Obter(uint usuarioId, int ano, int mes, out string errorMsg)
        {
            errorMsg = string.Empty;
            if (usuarioId == 0 || mes < 1 || mes > 12 || ano < 2000 || ano > 2100)
            {
                errorMsg = "O período informado para o relatório é inválido.";
                return new RelatoriosViewModel { AnoReferencia = ano, MesReferencia = mes };
            }

            DateTime inicioMes = new DateTime(ano, mes, 1);
            DateTime inicioProximoMes = inicioMes.AddMonths(1);
            DateTime inicioMesAnterior = inicioMes.AddMonths(-1);
            DateTime inicioEvolucao = inicioMes.AddMonths(-11);
            DateTime hoje = DateTime.Today;
            int diasNoMes = DateTime.DaysInMonth(ano, mes);
            int diasDecorridos = inicioMes > new DateTime(hoje.Year, hoje.Month, 1)
                ? 0
                : (ano == hoje.Year && mes == hoje.Month ? hoje.Day : diasNoMes);

            var relatorio = new RelatoriosViewModel
            {
                AnoReferencia = ano,
                MesReferencia = mes,
                PeriodoNome = inicioMes.ToString("MMMM yyyy", new CultureInfo("pt-BR")),
                MesAtual = ano == hoje.Year && mes == hoje.Month,
                DiasNoMes = diasNoMes,
                DiasDecorridos = diasDecorridos
            };

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
                    t.DOC_AIC AS DocumentoOrigemId,
                    COALESCE(d.DOC_NOM, '') AS NomeArquivoOrigem,
                    t.TRN_CNF AS ConfiancaIA,
                    t.TRN_PAG AS PaginaOrigem,
                    COALESCE(t.TRN_PLA, '') AS PlanilhaOrigem,
                    t.TRN_LIN AS LinhaOrigem
                FROM trn t
                LEFT JOIN doc d ON d.DOC_AIC = t.DOC_AIC AND d.USR_AIC = t.USR_AIC
                WHERE t.USR_AIC = @UsuarioId
                    AND t.TRN_STA = @StatusConcluido
                    AND (
                        (t.TRN_DTA >= @InicioEvolucao AND t.TRN_DTA < @InicioProximoMes)
                        OR (t.TRN_DTA IS NULL AND d.DOC_DTU >= @InicioMes AND d.DOC_DTU < @InicioProximoMes)
                    )
                ORDER BY t.TRN_DTA DESC, t.TRN_AIC DESC";

            var transacoes = ConexaoBD.Consulta<Transacao>(sqlTransacoes, new Dictionary<string, object>
            {
                { "UsuarioId", usuarioId },
                { "StatusConcluido", (int)StatusTransacao.Concluido },
                { "InicioEvolucao", inicioEvolucao },
                { "InicioMes", inicioMes },
                { "InicioProximoMes", inicioProximoMes }
            }, out errorMsg);

            if (!string.IsNullOrEmpty(errorMsg) || transacoes == null)
            {
                return relatorio;
            }

            AplicarCategorias(transacoes);
            var transacoesPeriodo = transacoes
                .Where(item => item.DataTransacao >= inicioMes && item.DataTransacao < inicioProximoMes)
                .ToList();
            var transacoesMesAnterior = transacoes
                .Where(item => item.DataTransacao >= inicioMesAnterior && item.DataTransacao < inicioMes)
                .ToList();
            var transacoesComData = transacoes.Where(item => item.DataTransacao.HasValue).ToList();
            var despesasPeriodo = transacoesPeriodo.Where(item => item.Tipo == TipoTransacao.Despesa).ToList();
            var receitasPeriodo = transacoesPeriodo.Where(item => item.Tipo == TipoTransacao.Receita).ToList();
            var despesasMesAnterior = transacoesMesAnterior.Where(item => item.Tipo == TipoTransacao.Despesa).ToList();
            var receitasMesAnterior = transacoesMesAnterior.Where(item => item.Tipo == TipoTransacao.Receita).ToList();

            relatorio.TotalReceitas = receitasPeriodo.Sum(item => item.Valor);
            relatorio.TotalDespesas = despesasPeriodo.Sum(item => item.Valor);
            relatorio.ReceitasMesAnterior = receitasMesAnterior.Sum(item => item.Valor);
            relatorio.DespesasMesAnterior = despesasMesAnterior.Sum(item => item.Valor);
            relatorio.QuantidadeReceitas = receitasPeriodo.Count;
            relatorio.QuantidadeDespesas = despesasPeriodo.Count;
            relatorio.TemHistorico = transacoesComData.Count > 0;
            relatorio.Manchete = relatorio.TotalLancamentos == 0
                ? "Sem lançamentos neste período"
                : (relatorio.SaldoPeriodo > 0 ? "Resultado positivo no período"
                    : (relatorio.SaldoPeriodo < 0 ? "Despesas acima das receitas" : "Período equilibrado"));
            relatorio.LancamentosRecentes = transacoesPeriodo
                .OrderByDescending(item => item.DataTransacao)
                .ThenByDescending(item => item.Id)
                .Take(100)
                .ToList();

            PreencherSaldoAcumulado(relatorio, usuarioId, inicioProximoMes, out errorMsg);
            if (!string.IsNullOrEmpty(errorMsg)) return relatorio;

            decimal saldoAnteriorEvolucao = ObterSaldoAnterior(usuarioId, inicioEvolucao, out errorMsg);
            if (!string.IsNullOrEmpty(errorMsg)) return relatorio;

            PreencherEvolucao(relatorio, transacoesComData, inicioEvolucao, inicioMes, saldoAnteriorEvolucao);
            PreencherCategorias(relatorio, despesasPeriodo, receitasPeriodo, despesasMesAnterior);
            PreencherDadosDiarios(relatorio, despesasPeriodo, receitasPeriodo, despesasMesAnterior, inicioMes, inicioMesAnterior);
            PreencherRankings(relatorio, despesasPeriodo);
            PreencherQualidadeIA(relatorio, transacoesPeriodo, transacoes.Where(item => !item.DataTransacao.HasValue));
            CalcularScoreEInsights(relatorio);

            var documentos = transacoesPeriodo
                .Where(item => item.DocumentoOrigemId.HasValue)
                .Select(item => item.DocumentoOrigemId.Value)
                .Distinct()
                .Count();
            relatorio.TotalDocumentos = documentos;

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
                    COALESCE(d.DOC_NOM, '') AS NomeArquivoOrigem,
                    t.TRN_CNF AS ConfiancaIA,
                    t.TRN_PAG AS PaginaOrigem,
                    COALESCE(t.TRN_PLA, '') AS PlanilhaOrigem,
                    t.TRN_LIN AS LinhaOrigem
                FROM trn t
                LEFT JOIN doc d ON d.DOC_AIC = t.DOC_AIC AND d.USR_AIC = t.USR_AIC
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

            if (string.IsNullOrEmpty(errorMsg) && transacoes != null)
            {
                AplicarCategorias(transacoes);
            }

            return transacoes;
        }

        private static void PreencherSaldoAcumulado(RelatoriosViewModel relatorio, uint usuarioId, DateTime fim, out string errorMsg)
        {
            const string sql = @"
                SELECT
                    COALESCE(SUM(CASE WHEN TRN_TIP = 1 THEN TRN_VAL WHEN TRN_TIP = 2 THEN -TRN_VAL ELSE 0 END), 0) AS Saldo,
                    COUNT(*) AS Quantidade
                FROM trn
                WHERE USR_AIC = @UsuarioId
                    AND TRN_STA = @StatusConcluido
                    AND TRN_DTA < @Fim";

            var resumo = ConexaoBD.ConsultaObjeto<SaldoRelatorioBD>(sql, new Dictionary<string, object>
            {
                { "UsuarioId", usuarioId },
                { "StatusConcluido", (int)StatusTransacao.Concluido },
                { "Fim", fim }
            }, out errorMsg);

            if (string.IsNullOrEmpty(errorMsg) && resumo != null)
            {
                relatorio.SaldoAcumulado = resumo.Saldo;
                relatorio.TemHistorico = resumo.Quantidade > 0;
            }
        }

        private static decimal ObterSaldoAnterior(uint usuarioId, DateTime inicio, out string errorMsg)
        {
            const string sql = @"
                SELECT COALESCE(SUM(CASE WHEN TRN_TIP = 1 THEN TRN_VAL WHEN TRN_TIP = 2 THEN -TRN_VAL ELSE 0 END), 0) AS Saldo
                FROM trn
                WHERE USR_AIC = @UsuarioId
                    AND TRN_STA = @StatusConcluido
                    AND TRN_DTA < @Inicio";

            var resumo = ConexaoBD.ConsultaObjeto<SaldoRelatorioBD>(sql, new Dictionary<string, object>
            {
                { "UsuarioId", usuarioId },
                { "StatusConcluido", (int)StatusTransacao.Concluido },
                { "Inicio", inicio }
            }, out errorMsg);

            return resumo == null ? 0 : resumo.Saldo;
        }

        private static void PreencherEvolucao(RelatoriosViewModel relatorio, List<Transacao> transacoes, DateTime inicio, DateTime mesReferencia, decimal saldoAcumulado)
        {
            var mensal = transacoes
                .Where(item => item.DataTransacao >= inicio && item.DataTransacao < mesReferencia.AddMonths(1))
                .GroupBy(item => new { item.DataTransacao.Value.Year, item.DataTransacao.Value.Month })
                .ToDictionary(grupo => new DateTime(grupo.Key.Year, grupo.Key.Month, 1), grupo => new
                {
                    Receitas = grupo.Where(item => item.Tipo == TipoTransacao.Receita).Sum(item => item.Valor),
                    Despesas = grupo.Where(item => item.Tipo == TipoTransacao.Despesa).Sum(item => item.Valor)
                });

            for (int indice = 0; indice < 12; indice++)
            {
                DateTime dataMes = inicio.AddMonths(indice);
                var valores = mensal.ContainsKey(dataMes) ? mensal[dataMes] : null;
                decimal receitas = valores == null ? 0 : valores.Receitas;
                decimal despesas = valores == null ? 0 : valores.Despesas;
                saldoAcumulado += receitas - despesas;

                relatorio.EvolucaoMensal.Add(new RelatorioMesViewModel
                {
                    Ano = dataMes.Year,
                    Mes = dataMes.Month,
                    Nome = dataMes.ToString("MMM", new CultureInfo("pt-BR")),
                    Receitas = receitas,
                    Despesas = despesas,
                    SaldoAcumulado = saldoAcumulado
                });
            }

            decimal maiorValor = relatorio.EvolucaoMensal
                .Select(item => Math.Max(item.Receitas, item.Despesas))
                .DefaultIfEmpty(0)
                .Max();
            foreach (var item in relatorio.EvolucaoMensal)
            {
                item.PercentualReceitas = maiorValor > 0 ? (int)Math.Round(item.Receitas / maiorValor * 100) : 0;
                item.PercentualDespesas = maiorValor > 0 ? (int)Math.Round(item.Despesas / maiorValor * 100) : 0;
            }
        }

        private static void PreencherCategorias(RelatoriosViewModel relatorio, List<Transacao> despesas, List<Transacao> receitas, List<Transacao> despesasAnterior)
        {
            var catalogo = CatalogoCategorias.ObterCategorias();
            relatorio.DespesasPorCategoria = AgruparCategorias(despesas, catalogo);
            relatorio.ReceitasPorCategoria = AgruparCategorias(receitas, catalogo);

            var gruposAnteriores = despesasAnterior
                .GroupBy(item => item.CategoriaCodigo ?? string.Empty)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(item => item.Valor));
            var categoriasAtuais = relatorio.DespesasPorCategoria.ToDictionary(item => item.Codigo, item => item);
            var codigos = categoriasAtuais.Keys.Union(gruposAnteriores.Keys).ToList();
            decimal maiorCategoria = codigos
                .Select(codigo => categoriasAtuais.ContainsKey(codigo) ? categoriasAtuais[codigo].Valor : 0)
                .DefaultIfEmpty(0)
                .Max();

            foreach (string codigo in codigos)
            {
                RelatorioCategoriaViewModel atual;
                if (!categoriasAtuais.TryGetValue(codigo, out atual))
                {
                    var categoria = CriarCategoria(codigo, catalogo, 0, 0);
                    relatorio.ComparativoDespesas.Add(categoria);
                    atual = categoria;
                }

                atual.ValorAnterior = gruposAnteriores.ContainsKey(codigo) ? gruposAnteriores[codigo] : 0;
                atual.Percentual = maiorCategoria > 0 ? (int)Math.Round(atual.Valor / maiorCategoria * 100) : 0;
                relatorio.ComparativoDespesas.Add(atual);
            }

            relatorio.ComparativoDespesas = relatorio.ComparativoDespesas
                .OrderByDescending(item => item.Valor)
                .ThenBy(item => item.Nome)
                .ToList();
        }

        private static List<RelatorioCategoriaViewModel> AgruparCategorias(List<Transacao> transacoes, List<CategoriaFixa> catalogo)
        {
            var resultado = transacoes
                .GroupBy(item => item.CategoriaCodigo ?? string.Empty)
                .Select(grupo => CriarCategoria(grupo.Key, catalogo, grupo.Sum(item => item.Valor), grupo.Count()))
                .OrderByDescending(item => item.Valor)
                .ToList();
            decimal total = resultado.Sum(item => item.Valor);
            foreach (var item in resultado)
            {
                item.Percentual = total > 0 ? (int)Math.Round(item.Valor / total * 100) : 0;
            }

            return resultado;
        }

        private static RelatorioCategoriaViewModel CriarCategoria(string codigo, List<CategoriaFixa> catalogo, decimal valor, int quantidade)
        {
            var categoria = catalogo.FirstOrDefault(item => item.Codigo == codigo);
            int indice = catalogo.FindIndex(item => item.Codigo == codigo);
            if (indice < 0) indice = CoresCategorias.Length - 1;
            indice %= CoresCategorias.Length;

            return new RelatorioCategoriaViewModel
            {
                Codigo = codigo,
                Nome = categoria == null ? "Sem categoria" : categoria.Nome,
                Cor = CoresCategorias[indice],
                Valor = valor,
                Quantidade = quantidade
            };
        }

        private static void PreencherDadosDiarios(RelatoriosViewModel relatorio, List<Transacao> despesas, List<Transacao> receitas, List<Transacao> despesasAnterior, DateTime inicioMes, DateTime inicioMesAnterior)
        {
            var gastosPorDia = despesas
                .Where(item => item.DataTransacao.HasValue)
                .GroupBy(item => item.DataTransacao.Value.Day)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(item => item.Valor));
            var receitasPorDia = receitas
                .Where(item => item.DataTransacao.HasValue)
                .GroupBy(item => item.DataTransacao.Value.Day)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(item => item.Valor));
            var anteriorPorDia = despesasAnterior
                .Where(item => item.DataTransacao.HasValue)
                .GroupBy(item => item.DataTransacao.Value.Day)
                .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(item => item.Valor));

            decimal acumuladoAtual = 0;
            decimal acumuladoAnterior = 0;
            int diasMes = relatorio.DiasNoMes;
            int diasMesAnterior = DateTime.DaysInMonth(inicioMesAnterior.Year, inicioMesAnterior.Month);
            decimal maiorGastoDiario = gastosPorDia.Values.DefaultIfEmpty(0).Max();
            var gastosPorSemana = Enumerable.Range(0, 7).Select(_ => 0m).ToArray();
            var ocorrenciasDiaSemana = Enumerable.Range(0, 7).Select(_ => 0).ToArray();

            for (int dia = 1; dia <= diasMes; dia++)
            {
                DateTime data = inicioMes.AddDays(dia - 1);
                decimal gasto = gastosPorDia.ContainsKey(dia) ? gastosPorDia[dia] : 0;
                decimal receita = receitasPorDia.ContainsKey(dia) ? receitasPorDia[dia] : 0;
                acumuladoAtual += gasto;

                if (dia <= diasMesAnterior)
                {
                    acumuladoAnterior += anteriorPorDia.ContainsKey(dia) ? anteriorPorDia[dia] : 0;
                    relatorio.AcumuladoDespesasAnterior.Add(acumuladoAnterior);
                }
                else
                {
                    relatorio.AcumuladoDespesasAnterior.Add(null);
                }

                relatorio.AcumuladoDespesas.Add(acumuladoAtual);
                relatorio.Dias.Add(new RelatorioDiaViewModel
                {
                    Dia = dia,
                    Data = data,
                    Receitas = receita,
                    Despesas = gasto,
                    Quantidade = despesas.Count(item => item.DataTransacao.HasValue && item.DataTransacao.Value.Date == data.Date)
                        + receitas.Count(item => item.DataTransacao.HasValue && item.DataTransacao.Value.Date == data.Date),
                    Nivel = maiorGastoDiario > 0 ? (int)Math.Ceiling(gasto / maiorGastoDiario * 4) : 0
                });

                int diaSemana = (int)data.DayOfWeek;
                gastosPorSemana[diaSemana] += gasto;
                ocorrenciasDiaSemana[diaSemana]++;
            }

            for (int diaSemana = 0; diaSemana < 7; diaSemana++)
            {
                relatorio.GastoMedioPorDiaSemana.Add(ocorrenciasDiaSemana[diaSemana] > 0
                    ? gastosPorSemana[diaSemana] / ocorrenciasDiaSemana[diaSemana]
                    : 0);
            }

            int semanaMaisCara = Array.IndexOf(gastosPorSemana, gastosPorSemana.Max());
            relatorio.DiaSemanaMaisCaro = new[] { "domingo", "segunda-feira", "terça-feira", "quarta-feira", "quinta-feira", "sexta-feira", "sábado" }[semanaMaisCara];

            int diasConsiderados = Math.Max(1, relatorio.DiasDecorridos);
            relatorio.MediaDiariaDespesas = relatorio.TotalDespesas / diasConsiderados;
            relatorio.MediaDiariaMesAnterior = relatorio.DespesasMesAnterior / diasMesAnterior;
            if (relatorio.MesAtual)
            {
                relatorio.ProjecaoDespesas = Math.Round(relatorio.MediaDiariaDespesas * diasMes, 2);
            }
        }

        private static void PreencherRankings(RelatoriosViewModel relatorio, List<Transacao> despesas)
        {
            var grupos = despesas
                .GroupBy(item => NormalizarDescricao(item.Descricao))
                .Select(grupo => new RelatorioRankingViewModel
                {
                    Nome = grupo.First().Descricao,
                    Valor = grupo.Sum(item => item.Valor),
                    Quantidade = grupo.Count()
                })
                .OrderByDescending(item => item.Valor)
                .Take(5)
                .ToList();

            decimal total = despesas.Sum(item => item.Valor);
            foreach (var item in grupos)
            {
                item.Percentual = total > 0 ? (int)Math.Round(item.Valor / total * 100) : 0;
            }

            relatorio.TopEstabelecimentos = grupos;
            relatorio.MaioresDespesas = despesas
                .OrderByDescending(item => item.Valor)
                .ThenByDescending(item => item.DataTransacao)
                .Take(5)
                .ToList();
        }

        private static void PreencherQualidadeIA(RelatoriosViewModel relatorio, List<Transacao> transacoesPeriodo, IEnumerable<Transacao> semData)
        {
            var transacoesIA = transacoesPeriodo.Where(item => item.CriadoPorIA).ToList();
            relatorio.LancamentosIA = transacoesIA.Count;
            relatorio.ConfiancaMediaIA = transacoesIA.Count > 0
                ? (int)Math.Round(transacoesIA.Average(item => item.ConfiancaIA) * 100)
                : 0;
            relatorio.LancamentosBaixaConfianca = transacoesIA.Count(item => item.ConfiancaIA < 0.7m);
            relatorio.LancamentosSemData = semData.Count(item => item.CriadoPorIA);
        }

        private static void CalcularScoreEInsights(RelatoriosViewModel relatorio)
        {
            if (relatorio.TotalLancamentos == 0) return;

            int pontosPoupanca;
            decimal taxaPoupanca = relatorio.TotalReceitas > 0
                ? (relatorio.TotalReceitas - relatorio.TotalDespesas) / relatorio.TotalReceitas * 100m
                : -100m;
            if (taxaPoupanca >= 20) pontosPoupanca = 40;
            else if (taxaPoupanca >= 10) pontosPoupanca = 30;
            else if (taxaPoupanca > 0) pontosPoupanca = 20;
            else if (taxaPoupanca == 0) pontosPoupanca = 10;
            else pontosPoupanca = 0;

            int pontosDespesas;
            if (relatorio.DespesasMesAnterior <= 0)
            {
                pontosDespesas = relatorio.TotalDespesas > 0 ? 25 : 35;
            }
            else
            {
                decimal variacao = relatorio.TotalDespesas / relatorio.DespesasMesAnterior;
                pontosDespesas = variacao <= 1 ? 35 : (variacao <= 1.1m ? 25 : (variacao <= 1.25m ? 15 : 5));
            }

            int pontosConfianca = relatorio.LancamentosIA == 0
                ? 25
                : (int)Math.Round(relatorio.ConfiancaMediaIA / 100m * 25);
            relatorio.ScoreFinanceiro = pontosPoupanca + pontosDespesas + pontosConfianca;
            relatorio.ScoreRotulo = relatorio.ScoreFinanceiro >= 80 ? "Saudável"
                : (relatorio.ScoreFinanceiro >= 55 ? "Atenção" : "Crítico");
            relatorio.ScoreCor = relatorio.ScoreFinanceiro >= 80 ? "#10b981"
                : (relatorio.ScoreFinanceiro >= 55 ? "#f59e0b" : "#f43f5e");
            relatorio.ScoreItens.Add(new ScoreItemViewModel
            {
                Nome = "Poupança do período",
                Dica = relatorio.TotalReceitas > 0 ? "Parte da renda que restou após as despesas." : "Não houve receita registrada neste período.",
                Pontos = pontosPoupanca,
                Maximo = 40
            });
            relatorio.ScoreItens.Add(new ScoreItemViewModel
            {
                Nome = "Variação das despesas",
                Dica = relatorio.DespesasMesAnterior > 0 ? "Comparação com o mês anterior." : "Sem despesas anteriores para comparar.",
                Pontos = pontosDespesas,
                Maximo = 35
            });
            relatorio.ScoreItens.Add(new ScoreItemViewModel
            {
                Nome = "Confiança da leitura",
                Dica = relatorio.LancamentosIA > 0 ? "Média de confiança informada pela análise dos arquivos." : "Não há lançamentos de IA neste período.",
                Pontos = pontosConfianca,
                Maximo = 25
            });

            if (relatorio.TotalReceitas > 0)
            {
                relatorio.Insights.Add(new InsightViewModel
                {
                    Nivel = taxaPoupanca >= 0 ? "success" : "warning",
                    Titulo = taxaPoupanca >= 0 ? "Resultado positivo" : "Despesas acima das receitas",
                    Texto = "O período fechou com " + relatorio.SaldoFormatado + " de resultado líquido."
                });
            }

            if (relatorio.DespesasMesAnterior > 0)
            {
                decimal variacao = (relatorio.TotalDespesas - relatorio.DespesasMesAnterior) / relatorio.DespesasMesAnterior * 100m;
                relatorio.Insights.Add(new InsightViewModel
                {
                    Nivel = variacao > 10 ? "warning" : "info",
                    Titulo = variacao > 0 ? "Despesas em alta" : (variacao < 0 ? "Despesas em queda" : "Despesas estáveis"),
                    Texto = "As despesas variaram " + Math.Abs(variacao).ToString("0.#", CultureInfo.GetCultureInfo("pt-BR")) + "% em relação ao mês anterior."
                });
            }

            if (relatorio.DespesasPorCategoria.Count > 0)
            {
                var maiorCategoria = relatorio.DespesasPorCategoria.OrderByDescending(item => item.Valor).First();
                relatorio.Insights.Add(new InsightViewModel
                {
                    Nivel = "info",
                    Titulo = "Maior categoria de despesas",
                    Texto = maiorCategoria.Nome + " concentrou " + maiorCategoria.Percentual + "% das despesas do período."
                });
            }

            if (relatorio.LancamentosBaixaConfianca > 0 || relatorio.LancamentosSemData > 0)
            {
                relatorio.Insights.Add(new InsightViewModel
                {
                    Nivel = "warning",
                    Titulo = "Lançamentos para revisar",
                    Texto = (relatorio.LancamentosBaixaConfianca + relatorio.LancamentosSemData) + " lançamento(s) precisam de conferência."
                });
            }
        }

        private static string NormalizarDescricao(string descricao)
        {
            return string.Join(" ", (descricao ?? string.Empty)
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();
        }

        private static void AplicarCategorias(List<Transacao> transacoes)
        {
            var catalogo = CatalogoCategorias.ObterCategorias();
            foreach (var transacao in transacoes)
            {
                var categoria = catalogo.FirstOrDefault(item => item.Codigo == transacao.CategoriaCodigo);
                transacao.CategoriaNome = categoria == null ? "Sem categoria" : categoria.Nome;
            }
        }
    }

    public class PeriodoRelatorioBD
    {
        public int Ano { get; set; }
        public int Mes { get; set; }
    }

    public class SaldoRelatorioBD
    {
        public decimal Saldo { get; set; }
        public int Quantidade { get; set; }
    }
}
