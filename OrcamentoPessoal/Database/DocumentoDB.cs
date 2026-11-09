using System.Collections.Generic;

namespace OrcamentoPessoal.Database
{
    public class DocumentoDB
    {
        public static void Registrar(uint usuarioId, string nomeArquivo, string extensao, long tamanhoBytes, string identificador, out string errorMsg)
        {
            const string sql = @"
                INSERT INTO doc
                    (USR_AIC, DOC_NOM, DOC_EXT, DOC_TAM, DOC_ID, DOC_STA)
                VALUES
                    (@UsuarioId, @NomeArquivo, @Extensao, @TamanhoBytes, @Identificador, 'Recebido')
                ON DUPLICATE KEY UPDATE
                    DOC_NOM = IF(USR_AIC = @UsuarioId, VALUES(DOC_NOM), DOC_NOM),
                    DOC_EXT = IF(USR_AIC = @UsuarioId, VALUES(DOC_EXT), DOC_EXT),
                    DOC_TAM = IF(USR_AIC = @UsuarioId, VALUES(DOC_TAM), DOC_TAM),
                    DOC_STA = IF(USR_AIC = @UsuarioId, 'Recebido', DOC_STA)";

            var parametros = new Dictionary<string, object>
            {
                { "UsuarioId", usuarioId },
                { "NomeArquivo", nomeArquivo },
                { "Extensao", extensao },
                { "TamanhoBytes", tamanhoBytes },
                { "Identificador", identificador }
            };

            ConexaoBD.ExecutarComando(sql, parametros, out errorMsg);
        }

        public static bool JaFoiImportado(uint usuarioId, string identificador, out string errorMsg)
        {
            const string sql = @"
                SELECT DOC_STA AS Status
                FROM doc
                WHERE USR_AIC = @UsuarioId
                    AND DOC_ID = @Identificador
                LIMIT 1";

            var parametros = new Dictionary<string, object>
            {
                { "UsuarioId", usuarioId },
                { "Identificador", identificador }
            };

            var documento = ConexaoBD.ConsultaObjeto<DocumentoStatusBD>(sql, parametros, out errorMsg);
            return documento != null && (documento.Status == "Importado" || documento.Status == "Analisado");
        }

        public static void AtualizarStatus(uint usuarioId, string identificador, string status, out string errorMsg)
        {
            const string sql = @"
                UPDATE doc
                SET DOC_STA = @Status
                WHERE USR_AIC = @UsuarioId
                    AND DOC_ID = @Identificador";

            var parametros = new Dictionary<string, object>
            {
                { "Status", status },
                { "UsuarioId", usuarioId },
                { "Identificador", identificador }
            };

            ConexaoBD.ExecutarComando(sql, parametros, out errorMsg);
        }

        public static DocumentoIdBD ObterId(uint usuarioId, string identificador, out string errorMsg)
        {
            const string sql = @"
                SELECT DOC_AIC AS Id
                FROM doc
                WHERE USR_AIC = @UsuarioId
                    AND DOC_ID = @Identificador
                LIMIT 1";

            var parametros = new Dictionary<string, object>
            {
                { "UsuarioId", usuarioId },
                { "Identificador", identificador }
            };

            return ConexaoBD.ConsultaObjeto<DocumentoIdBD>(sql, parametros, out errorMsg);
        }
    }

    public class DocumentoStatusBD
    {
        public string Status { get; set; }
    }
}
