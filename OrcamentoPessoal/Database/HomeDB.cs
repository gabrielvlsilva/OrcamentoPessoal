using System;
using System.Collections.Generic;
using OrcamentoPessoal.Models;

namespace OrcamentoPessoal.Database
{
    public class HomeDB
    {
        public static DashboardViewModel ObterDadosDashboard(Usuario usuario = null)
        {
            var user = usuario ?? new Usuario
            {
                Nome = "Usuário",
                Perfil = "Usuário"
            };

            var vm = new DashboardViewModel
            {
                Usuario = user,
                SaldoAtual = 8450.20m,
                TotalReceitasMes = 12800.00m,
                TotalDespesasMes = 4349.80m,
                EconomiaPrevistaMes = 8450.20m,
                TotalDocumentosProcessados = 6,
                TotalTransacoesExtraidasIA = 42,
                UltimasTransacoes = ObterTransacoesMock(),
                DocumentosRecentes = ObterDocumentosMock(),
                CategoriasResumo = ObterCategoriasMock()
            };

            return vm;
        }

        public static List<Transacao> ObterTransacoesMock()
        {
            return new List<Transacao>
            {
                new Transacao
                {
                    Id = 1,
                    Descricao = "Salário Mensal - Empresa Tech",
                    Valor = 9500.00m,
                    DataTransacao = DateTime.Today.AddDays(-2),
                    Tipo = TipoTransacao.Receita,
                    CategoriaNome = "Remuneração",
                    CategoriaIcone = "bi-cash-coin",
                    CategoriaCor = "#10b981",
                    FormaPagamento = "TED / Pix",
                    Status = StatusTransacao.Concluido,
                    Observacao = "Depósito mensal em conta corrente"
                },
                new Transacao
                {
                    Id = 2,
                    Descricao = "Supermercado Pão de Açúcar",
                    Valor = 684.50m,
                    DataTransacao = DateTime.Today.AddDays(-3),
                    Tipo = TipoTransacao.Despesa,
                    CategoriaNome = "Alimentação",
                    CategoriaIcone = "bi-cart3",
                    CategoriaCor = "#f59e0b",
                    FormaPagamento = "Cartão de Crédito",
                    Status = StatusTransacao.Concluido,
                    DocumentoOrigemId = 101,
                    NomeArquivoOrigem = "Fatura_Nubank_Mar2025.pdf",
                    Observacao = "Itens identificados automaticamente por IA"
                },
                new Transacao
                {
                    Id = 3,
                    Descricao = "Aluguel & Condomínio",
                    Valor = 1950.00m,
                    DataTransacao = DateTime.Today.AddDays(-5),
                    Tipo = TipoTransacao.Despesa,
                    CategoriaNome = "Moradia",
                    CategoriaIcone = "bi-house-door",
                    CategoriaCor = "#6366f1",
                    FormaPagamento = "Boleto Bancário",
                    Status = StatusTransacao.Concluido,
                    Observacao = "Pagamento pontual com desconto"
                },
                new Transacao
                {
                    Id = 4,
                    Descricao = "Projeto Freelance - Dashboard Web",
                    Valor = 3300.00m,
                    DataTransacao = DateTime.Today.AddDays(-7),
                    Tipo = TipoTransacao.Receita,
                    CategoriaNome = "Freelance",
                    CategoriaIcone = "bi-laptop",
                    CategoriaCor = "#10b981",
                    FormaPagamento = "Pix",
                    Status = StatusTransacao.Concluido,
                    Observacao = "Entrega módulo de relatórios"
                },
                new Transacao
                {
                    Id = 5,
                    Descricao = "Posto Ipiranga - Combustível",
                    Valor = 220.00m,
                    DataTransacao = DateTime.Today.AddDays(-8),
                    Tipo = TipoTransacao.Despesa,
                    CategoriaNome = "Transporte",
                    CategoriaIcone = "bi-fuel-pump",
                    CategoriaCor = "#06b6d4",
                    FormaPagamento = "Cartão de Débito",
                    Status = StatusTransacao.Concluido,
                    DocumentoOrigemId = 102,
                    NomeArquivoOrigem = "Extrato_Itau_Fev2025.pdf",
                    Observacao = "Lançamento conciliado via IA"
                },
                new Transacao
                {
                    Id = 6,
                    Descricao = "Assinatura OpenAI & Plataformas Cloud",
                    Valor = 195.30m,
                    DataTransacao = DateTime.Today.AddDays(-10),
                    Tipo = TipoTransacao.Despesa,
                    CategoriaNome = "Tecnologia",
                    CategoriaIcone = "bi-robot",
                    CategoriaCor = "#8b5cf6",
                    FormaPagamento = "Cartão de Crédito",
                    Status = StatusTransacao.Concluido,
                    DocumentoOrigemId = 101,
                    NomeArquivoOrigem = "Fatura_Nubank_Mar2025.pdf",
                    Observacao = "Cobrança internacional convertida"
                }
            };
        }

        public static List<DocumentoFinanceiro> ObterDocumentosMock()
        {
            return new List<DocumentoFinanceiro>
            {
                new DocumentoFinanceiro
                {
                    Id = 101,
                    NomeArquivo = "Fatura_Nubank_Mar2025.pdf",
                    Formato = FormatoArquivo.PDF,
                    Extensao = ".pdf",
                    TamanhoBytes = 245760,
                    TamanhoFormatado = "240 KB",
                    DataUpload = DateTime.Today.AddDays(-1),
                    Status = StatusProcessamentoIA.Concluido,
                    QtdTransacoesExtraidas = 18,
                    ValorTotalDetectado = 1354.20m,
                    ConfiancaIA = 97,
                    ResumoIA = "18 lançamentos extraídos, 0 duplicidades, categorização com 97% de certeza."
                },
                new DocumentoFinanceiro
                {
                    Id = 102,
                    NomeArquivo = "Extrato_Itau_Fev2025.pdf",
                    Formato = FormatoArquivo.PDF,
                    Extensao = ".pdf",
                    TamanhoBytes = 512000,
                    TamanhoFormatado = "500 KB",
                    DataUpload = DateTime.Today.AddDays(-4),
                    Status = StatusProcessamentoIA.Concluido,
                    QtdTransacoesExtraidas = 24,
                    ValorTotalDetectado = 8920.00m,
                    ConfiancaIA = 99,
                    ResumoIA = "Extrato bancário consolidado. Saldo inicial e final validados perfeitamente."
                },
                new DocumentoFinanceiro
                {
                    Id = 103,
                    NomeArquivo = "Planilha_Controle_Anual.xlsx",
                    Formato = FormatoArquivo.Excel,
                    Extensao = ".xlsx",
                    TamanhoBytes = 114688,
                    TamanhoFormatado = "112 KB",
                    DataUpload = DateTime.Today.AddDays(-7),
                    Status = StatusProcessamentoIA.Concluido,
                    QtdTransacoesExtraidas = 48,
                    ValorTotalDetectado = 22150.00m,
                    ConfiancaIA = 95,
                    ResumoIA = "Planilha convertida em formato unificado para emissão de relatórios."
                },
                new DocumentoFinanceiro
                {
                    Id = 104,
                    NomeArquivo = "Comprovantes_Digitalizados_Mar2025.pdf",
                    Formato = FormatoArquivo.PDF,
                    Extensao = ".pdf",
                    TamanhoBytes = 3200000,
                    TamanhoFormatado = "3.1 MB",
                    DataUpload = DateTime.Now.AddMinutes(-30),
                    Status = StatusProcessamentoIA.EmProcessamento,
                    QtdTransacoesExtraidas = 0,
                    ValorTotalDetectado = 0m,
                    ConfiancaIA = 92,
                    ResumoIA = "Módulo OCR em execução: identificando notas fiscais e cupons escaneados..."
                }
            };
        }

        public static List<Categoria> ObterCategoriasMock()
        {
            return new List<Categoria>
            {
                new Categoria
                {
                    Id = 1,
                    Nome = "Moradia & Contas Fixas",
                    Tipo = TipoTransacao.Despesa,
                    CorHex = "#6366f1",
                    IconeCss = "bi-house-door",
                    OrcamentoMensal = 2500.00m,
                    TotalGastoMes = 1950.00m,
                    Descricao = "Aluguel, condomínio, luz, água e internet"
                },
                new Categoria
                {
                    Id = 2,
                    Nome = "Alimentação & Mercado",
                    Tipo = TipoTransacao.Despesa,
                    CorHex = "#f59e0b",
                    IconeCss = "bi-cart3",
                    OrcamentoMensal = 1200.00m,
                    TotalGastoMes = 684.50m,
                    Descricao = "Supermercado, feiras e delivery"
                },
                new Categoria
                {
                    Id = 3,
                    Nome = "Transporte & Mobilidade",
                    Tipo = TipoTransacao.Despesa,
                    CorHex = "#06b6d4",
                    IconeCss = "bi-fuel-pump",
                    OrcamentoMensal = 600.00m,
                    TotalGastoMes = 220.00m,
                    Descricao = "Combustível, transporte por aplicativo e estacionamento"
                },
                new Categoria
                {
                    Id = 4,
                    Nome = "Educação & Tecnologia",
                    Tipo = TipoTransacao.Despesa,
                    CorHex = "#8b5cf6",
                    IconeCss = "bi-laptop",
                    OrcamentoMensal = 500.00m,
                    TotalGastoMes = 195.30m,
                    Descricao = "Ferramentas de IA, cursos e faculdade"
                },
                new Categoria
                {
                    Id = 5,
                    Nome = "Lazer & Bem-estar",
                    Tipo = TipoTransacao.Despesa,
                    CorHex = "#ec4899",
                    IconeCss = "bi-controller",
                    OrcamentoMensal = 800.00m,
                    TotalGastoMes = 300.00m,
                    Descricao = "Passeios, streaming e restaurantes"
                }
            };
        }
    }
}
