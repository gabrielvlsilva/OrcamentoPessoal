using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OrcamentoPessoal.Models;

namespace OrcamentoPessoal.Database
{
    public class TransacaoDB
    {
        public static void RegistrarImportacao(uint usuarioId, string identificadorDocumento, IList<TransacaoSugeridaViewModel> transacoes, out string errorMsg)
        {
            errorMsg = string.Empty;

            if (usuarioId == 0 || string.IsNullOrWhiteSpace(identificadorDocumento) || transacoes == null)
            {
                errorMsg = "Os dados da importação são inválidos.";
                return;
            }

            string erroDocumento;
            var documento = DocumentoDB.ObterId(usuarioId, identificadorDocumento, out erroDocumento);
            if (!string.IsNullOrEmpty(erroDocumento))
            {
                errorMsg = erroDocumento;
                return;
            }

            if (documento == null || documento.Id <= 0)
            {
                errorMsg = "O documento não foi encontrado para este usuário.";
                return;
            }

            var comandos = new List<ComandoSqlBD>();

            if (transacoes.Count > 0)
            {
                var sql = new StringBuilder();
                var parametros = new Dictionary<string, object>();
                sql.Append(@"INSERT INTO trn
                    (USR_AIC, DOC_AIC, TRN_DES, TRN_VAL, TRN_DTA, TRN_TIP, TRN_CAT, TRN_FPG, TRN_STA,
                     TRN_CNF, TRN_PAG, TRN_PLA, TRN_LIN)
                    VALUES ");

                for (int indice = 0; indice < transacoes.Count; indice++)
                {
                    var item = transacoes[indice];
                    string erroValidacao = ValidarTransacao(item);
                    if (!string.IsNullOrEmpty(erroValidacao))
                    {
                        errorMsg = erroValidacao;
                        return;
                    }

                    TipoTransacao tipo;
                    if (!Enum.TryParse(item.Tipo, true, out tipo))
                    {
                        errorMsg = "A análise retornou um tipo de lançamento inválido.";
                        return;
                    }

                    if (indice > 0)
                    {
                        sql.Append(", ");
                    }

                    sql.Append("(@Usuario").Append(indice).Append(", @Documento").Append(indice)
                        .Append(", @Descricao").Append(indice).Append(", @Valor").Append(indice)
                        .Append(", @Data").Append(indice).Append(", @Tipo").Append(indice)
                        .Append(", @Categoria").Append(indice).Append(", @Pagamento").Append(indice)
                        .Append(", @Status").Append(indice).Append(", @Confianca").Append(indice)
                        .Append(", @Pagina").Append(indice).Append(", @Planilha").Append(indice)
                        .Append(", @Linha").Append(indice).Append(")");

                    parametros.Add("Usuario" + indice, usuarioId);
                    parametros.Add("Documento" + indice, documento.Id);
                    parametros.Add("Descricao" + indice, item.Descricao.Trim());
                    parametros.Add("Valor" + indice, item.Valor);
                    parametros.Add("Data" + indice, item.Data.HasValue ? (object)item.Data.Value.Date : null);
                    parametros.Add("Tipo" + indice, (int)tipo);
                    parametros.Add("Categoria" + indice, item.CategoriaCodigo);
                    parametros.Add("Pagamento" + indice, item.FormaPagamento ?? string.Empty);
                    parametros.Add("Status" + indice, (int)StatusTransacao.Concluido);
                    parametros.Add("Confianca" + indice, Convert.ToDecimal(item.Confianca, CultureInfo.InvariantCulture));
                    parametros.Add("Pagina" + indice, item.Origem == null ? null : (object)item.Origem.Pagina);
                    parametros.Add("Planilha" + indice, item.Origem == null ? null : item.Origem.Planilha);
                    parametros.Add("Linha" + indice, item.Origem == null ? null : (object)item.Origem.Linha);
                }

                comandos.Add(new ComandoSqlBD { Sql = sql.ToString(), Parametros = parametros });
            }

            comandos.Add(new ComandoSqlBD
            {
                Sql = @"UPDATE doc
                    SET DOC_STA = 'Importado'
                    WHERE DOC_AIC = @DocumentoId AND USR_AIC = @UsuarioId",
                Parametros = new Dictionary<string, object>
                {
                    { "DocumentoId", documento.Id },
                    { "UsuarioId", usuarioId }
                }
            });

            ConexaoBD.ExecutarComandosEmTransacao(comandos, out errorMsg);
        }

        public static List<Transacao> ObterImportadas(uint usuarioId, out string errorMsg)
        {
            const string sql = @"
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
                INNER JOIN doc d ON d.DOC_AIC = t.DOC_AIC
                WHERE t.USR_AIC = @UsuarioId
                    AND d.USR_AIC = @UsuarioId
                    AND t.DOC_AIC IS NOT NULL
                    AND t.TRN_STA = @StatusConcluido
                ORDER BY COALESCE(t.TRN_DTA, DATE(t.TRN_DTC)) DESC, t.TRN_AIC DESC";

            var transacoes = ConexaoBD.Consulta<Transacao>(sql, new Dictionary<string, object>
            {
                { "UsuarioId", usuarioId },
                { "StatusConcluido", (int)StatusTransacao.Concluido }
            }, out errorMsg);

            if (!string.IsNullOrEmpty(errorMsg) || transacoes == null)
            {
                return transacoes;
            }

            var categorias = CatalogoCategorias.ObterCategorias();
            foreach (var transacao in transacoes)
            {
                var categoria = categorias.Find(item => item.Codigo == transacao.CategoriaCodigo);
                if (categoria != null)
                {
                    transacao.CategoriaNome = categoria.Nome;
                }
            }

            return transacoes;
        }

        public static void AtualizarImportada(uint usuarioId, EditarTransacaoImportadaViewModel edicao, decimal valor, out string errorMsg)
        {
            errorMsg = string.Empty;
            if (usuarioId == 0 || edicao == null || edicao.Id <= 0 || valor <= 0 || valor > 9999999999999.99m)
            {
                errorMsg = "Os dados do lançamento são inválidos.";
                return;
            }

            var registro = ObterRegistroImportado(usuarioId, edicao.Id, out errorMsg);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                return;
            }

            if (registro == null)
            {
                errorMsg = "O lançamento importado não foi encontrado para este usuário.";
                return;
            }

            var categoria = CatalogoCategorias.ObterCategorias().Find(item => item.Codigo == edicao.CategoriaCodigo);
            if (categoria == null || categoria.Tipo != registro.Tipo.ToString())
            {
                errorMsg = "Selecione uma categoria compatível com o tipo do lançamento.";
                return;
            }

            const string sql = @"
                UPDATE trn
                SET TRN_DES = @Descricao,
                    TRN_VAL = @Valor,
                    TRN_DTA = @DataTransacao,
                    TRN_CAT = @CategoriaCodigo
                WHERE TRN_AIC = @Id
                    AND USR_AIC = @UsuarioId
                    AND DOC_AIC IS NOT NULL
                    AND TRN_STA = @StatusConcluido";

            var parametros = new Dictionary<string, object>
            {
                { "Descricao", edicao.Descricao.Trim() },
                { "Valor", valor },
                { "DataTransacao", edicao.DataTransacao.HasValue ? (object)edicao.DataTransacao.Value.Date : null },
                { "CategoriaCodigo", edicao.CategoriaCodigo },
                { "Id", edicao.Id },
                { "UsuarioId", usuarioId },
                { "StatusConcluido", (int)StatusTransacao.Concluido }
            };

            ConexaoBD.ExecutarComando(sql, parametros, out errorMsg);
        }

        public static void ExcluirImportada(uint usuarioId, int transacaoId, out string errorMsg)
        {
            errorMsg = string.Empty;
            if (usuarioId == 0 || transacaoId <= 0)
            {
                errorMsg = "O lançamento informado é inválido.";
                return;
            }

            var registro = ObterRegistroImportado(usuarioId, transacaoId, out errorMsg);
            if (!string.IsNullOrEmpty(errorMsg))
            {
                return;
            }

            if (registro == null)
            {
                errorMsg = "O lançamento importado não foi encontrado para este usuário.";
                return;
            }

            const string sql = @"
                DELETE FROM trn
                WHERE TRN_AIC = @Id
                    AND USR_AIC = @UsuarioId
                    AND DOC_AIC IS NOT NULL
                    AND TRN_STA = @StatusConcluido";

            var parametros = new Dictionary<string, object>
            {
                { "Id", transacaoId },
                { "UsuarioId", usuarioId },
                { "StatusConcluido", (int)StatusTransacao.Concluido }
            };

            ConexaoBD.ExecutarComando(sql, parametros, out errorMsg);
        }

        private static Transacao ObterRegistroImportado(uint usuarioId, int transacaoId, out string errorMsg)
        {
            const string sql = @"
                SELECT TRN_AIC AS Id, TRN_TIP AS Tipo
                FROM trn
                WHERE TRN_AIC = @Id
                    AND USR_AIC = @UsuarioId
                    AND DOC_AIC IS NOT NULL
                    AND TRN_STA = @StatusConcluido
                LIMIT 1";

            var parametros = new Dictionary<string, object>
            {
                { "Id", transacaoId },
                { "UsuarioId", usuarioId },
                { "StatusConcluido", (int)StatusTransacao.Concluido }
            };

            return ConexaoBD.ConsultaObjeto<Transacao>(sql, parametros, out errorMsg);
        }

        private static string ValidarTransacao(TransacaoSugeridaViewModel transacao)
        {
            if (transacao == null || string.IsNullOrWhiteSpace(transacao.Descricao) || transacao.Descricao.Trim().Length > 150 ||
                transacao.Valor <= 0 || transacao.Confianca < 0 || transacao.Confianca > 1 ||
                (!string.IsNullOrEmpty(transacao.FormaPagamento) && transacao.FormaPagamento.Length > 100))
            {
                return "A análise retornou um lançamento inválido.";
            }

            var categoria = CatalogoCategorias.ObterCategorias().Find(item => item.Codigo == transacao.CategoriaCodigo);
            if (categoria == null || categoria.Tipo != transacao.Tipo)
            {
                return "A análise retornou uma categoria incompatível com o tipo do lançamento.";
            }

            if (transacao.Origem != null && !string.IsNullOrEmpty(transacao.Origem.Planilha) && transacao.Origem.Planilha.Length > 255)
            {
                return "A análise retornou uma referência de planilha inválida.";
            }

            return string.Empty;
        }
    }

    public class DocumentoIdBD
    {
        public int Id { get; set; }
    }
}
