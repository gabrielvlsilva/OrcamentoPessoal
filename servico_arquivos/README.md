# Serviço de Análise de Arquivos

Serviço Python interno que recebe bytes de um arquivo, extrai conteúdo em memória e devolve o resultado ao chamador. Ele não grava o arquivo nem o resultado, não acessa o MariaDB e não contém regras de negócio do orçamento.

## Requisitos

- Python 3.10 ou superior
- Acesso ao `pip` para instalar as dependências
- Biblioteca oficial `groq`, instalada pelo `requirements.txt`

## Executar no Windows

Abra o PowerShell na pasta `servico_arquivos` e execute:

```powershell
py -3 -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
$env:PYTHONDONTWRITEBYTECODE = '1'
python -m uvicorn app.main:app --host 127.0.0.1 --port 8001
```

O serviço escuta somente em `127.0.0.1` e não exige token de autenticação. Não o exponha em uma interface de rede pública.

Para habilitar a categorização, configure a chave do Groq no ambiente do processo Python antes de iniciar o serviço:

```powershell
$env:GROQ_API_KEY = 'SUA_CHAVE_DO_GROQ'
# Opcional; o padrão é openai/gpt-oss-20b
$env:GROQ_MODEL = 'openai/gpt-oss-20b'
python -m uvicorn app.main:app --host 127.0.0.1 --port 8001
```

Não coloque a chave no código, no `Web.config` nem em uma requisição do navegador. O C# envia ao serviço apenas o catálogo fixo de categorias e os bytes do arquivo; o serviço chama o Groq usando a variável de ambiente. Sem `GROQ_API_KEY`, a análise retorna erro de configuração.

## Endpoints

- `GET http://127.0.0.1:8001/health` verifica se o processo está ativo.
- `POST http://127.0.0.1:8001/analyze` recebe o conteúdo bruto no corpo da requisição. Envie o cabeçalho `X-File-Extension` (por exemplo, `.pdf`). A requisição precisa informar `Content-Length`.

São aceitos PDF, CSV (UTF-8 ou Windows-1252) e XLSX, com limite de 10 MB. PDFs são limitados a 100 páginas; as extrações de CSV e XLSX a 20.000 linhas, e a saída total a 8 MB. O conteúdo extraído é enviado ao Groq para sugerir lançamentos no formato fixo da aplicação: data, descrição, valor, tipo, categoria, forma de pagamento, origem e confiança. O catálogo é definido em `CatalogoCategorias` no C#; a IA só pode selecionar categorias existentes e compatíveis com o tipo do lançamento. A resposta inclui avisos e indica quando a entrada foi limitada.

O serviço processa os bytes em memória e retorna o resultado diretamente ao MVC. Ele não abre arquivos de destino, não cria identificadores de armazenamento e não registra nomes, conteúdo ou resultado dos arquivos. Para categorizar, o conteúdo extraído é transmitido à API do Groq; considere isso ao usar documentos financeiros reais. O limite de entrada enviado ao modelo é 60.000 caracteres e a confiança é uma estimativa do modelo, não uma garantia de exatidão.

O serviço usa a biblioteca oficial Python do Groq, a mesma usada no exemplo de conexão. Erros de resposta da API são devolvidos ao MVC com o status e a mensagem, após ocultar padrões de chave. Para atualizar o ambiente virtual após instalar a nova dependência, execute `python -m pip install -r requirements.txt` com o ambiente ativado.

Para reduzir o risco de truncar respostas com muitos lançamentos, o texto extraído é enviado em lotes de até 20 linhas/itens e aproximadamente 8.000 caracteres por chamada. Cada chamada permite até 16.384 tokens de saída; o modelo GPT-OSS 20B aceita até 65.536 tokens de saída.

## Responsabilidade do MVC

A aplicação C# autentica e autoriza o usuário, mantém os bytes do arquivo em memória durante o processamento, persiste apenas os metadados do documento e os lançamentos extraídos pela análise, e atualiza o dashboard. O arquivo não é gravado pelo código da aplicação; o corpo da requisição ASP.NET também está configurado para permanecer em memória dentro do limite de upload. O vínculo ao usuário, os metadados, as categorias fixas, os lançamentos e o orçamento permanecem sob responsabilidade do C#.

Antes do primeiro envio, execute `OrcamentoPessoal/SQL Versoes/1.1.0.0.sql` no banco `orcamento_db` para criar a tabela de metadados `doc`. Para salvar os lançamentos extraídos, execute também `OrcamentoPessoal/SQL Versoes/1.2.0.0.sql`, que cria a tabela `trn`. Os scripts não são executados automaticamente pela aplicação.

O serviço não faz OCR de PDFs escaneados; nesses casos, informa que não encontrou texto extraível. Os lançamentos sugeridos devem ser revisados antes de uma futura etapa de confirmação e persistência.
