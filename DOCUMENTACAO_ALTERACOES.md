# Documentação das alterações

Teste prático — **Implementação do CPF do cliente** e **botão Beneficiários**.
Para executar o projeto e configurar o banco, veja o [`README.md`](README.md).

## Sumário

1. [Visão geral](#1-visão-geral)
2. [Banco de dados](#2-banco-de-dados)
3. [Camadas C# (DML / DAL / BLL)](#3-camadas-c-dml--dal--bll)
4. [Camada web (Controller, Models, Views, Scripts)](#4-camada-web)
5. [Fluxos de gravação](#5-fluxos-de-gravação)
6. [Regras de validação](#6-regras-de-validação)
7. [Lista de arquivos novos e alterados](#7-arquivos-novos-e-alterados)
8. [Decisões de projeto e pontos de atenção](#8-decisões-de-projeto-e-pontos-de-atenção)

---

## 1. Visão geral

| Requisito do enunciado | Como foi atendido |
|---|---|
| Campo **CPF** na tela de cadastro/alteração, no padrão visual dos demais | Campo `CPF` na linha *CPF \| Nacionalidade* (conforme o layout do PDF), `form-control`, mesmo padrão dos outros campos |
| CPF **obrigatório** | `required` no HTML, validação no JavaScript e `[Required]` no servidor |
| Formatação **999.999.999-99** | Máscara automática enquanto digita (`FI.Cpf.js`) |
| **Validar** o CPF (dígito verificador) | Mesmo algoritmo no JavaScript (`FI.Cpf.js`) e no servidor (`ValidadorCpf.cs`) |
| **Não permitir CPF duplicado** | Consulta prévia (`FI_SP_VerificaCliente`) + índice único no banco (`UX_CLIENTES_CPF`) |
| Novo campo `CPF` na tabela `CLIENTES` | Coluna `CLIENTES.CPF VARCHAR(14)` |
| Botão **Beneficiários** com pop-up (CPF e Nome) e **grid** com alterar/excluir | Modal Bootstrap com formulário + grid (`Forms.cshtml` + `FI.Beneficiarios.js`) |
| CPF do beneficiário: máscara, validação e **sem repetição para o mesmo cliente** | Validação no pop-up, no servidor e índice único `UX_BENEFICIARIOS_IDCLIENTE_CPF` |
| Beneficiário **gravado ao acionar "Salvar"** do cliente | A lista fica em memória no navegador e é enviada junto com o cliente; gravada na mesma transação |
| Tabela `BENEFICIARIOS` com `ID`, `CPF`, `NOME`, `IDCLIENTE` | Tabela criada com exatamente esses campos |

---

## 2. Banco de dados

### 2.1 Estrutura (arquivo `FI.AtividadeEntrevista\DAL\Scripts\Estrutura.sql`)

```sql
-- Coluna nova em CLIENTES
ALTER TABLE dbo.CLIENTES ADD CPF VARCHAR(14) NULL

-- Impede CPF repetido entre clientes (ignora clientes antigos sem CPF)
CREATE UNIQUE NONCLUSTERED INDEX UX_CLIENTES_CPF ON dbo.CLIENTES (CPF) WHERE CPF IS NOT NULL

-- Tabela nova
CREATE TABLE dbo.BENEFICIARIOS (
    ID        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BENEFICIARIOS PRIMARY KEY,
    CPF       VARCHAR(14) NOT NULL,
    NOME      VARCHAR(50) NOT NULL,
    IDCLIENTE BIGINT      NOT NULL CONSTRAINT FK_BENEFICIARIOS_CLIENTES
                                   REFERENCES dbo.CLIENTES (ID) ON DELETE CASCADE
)

-- Impede o mesmo CPF duas vezes para o mesmo cliente
CREATE UNIQUE NONCLUSTERED INDEX UX_BENEFICIARIOS_IDCLIENTE_CPF ON dbo.BENEFICIARIOS (IDCLIENTE, CPF)
```

Cada comando é protegido por verificação de existência (`IF COL_LENGTH(...) IS NULL`, `IF OBJECT_ID(...) IS NULL`, `IF NOT EXISTS (... sys.indexes ...)`), portanto o script é **idempotente**.
Se já existir uma tabela `BENEFICIARIOS` **vazia** sem `ID` identity, ela é recriada no padrão acima; se já estiver no padrão, é mantida.

### 2.2 Como o script é aplicado
- Os arquivos `.sql` são **recursos embutidos** (`EmbeddedResource`) na DLL `FI.AtividadeEntrevista`.
- Em `Global.asax.cs → Application_Start` é chamado `BoBancoDados.GarantirEstrutura()` → `DAL.EstruturaBanco.Garantir()`, que executa `Estrutura.sql` primeiro e depois cada procedure, separando os lotes pelo comando `GO`.
- Em caso de falha, é lançada uma exceção informando **qual script** falhou e o motivo.
- Alternativa manual: `Banco\Script_Banco_Completo.sql` (mesmo conteúdo, em um arquivo só).

### 2.3 Stored procedures

| Procedure | Situação | Parâmetros / comportamento |
|---|---|---|
| `FI_SP_IncClienteV2` | **Alterada** | + `@CPF`, + `@BENEFICIARIOS XML = NULL`. Em **uma transação**: insere o cliente, obtém o ID (`SCOPE_IDENTITY`) e insere os beneficiários. Retorna o ID. |
| `FI_SP_AltCliente` | **Alterada** | + `@CPF`, + `@BENEFICIARIOS XML = NULL`. Em **uma transação**: atualiza o cliente e, se `@BENEFICIARIOS` não for `NULL`, apaga os beneficiários do cliente e reinsere a lista recebida. |
| `FI_SP_ConsCliente` | **Alterada** | Passa a retornar a coluna `CPF`. |
| `FI_SP_PesqCliente` | **Alterada** | Passa a retornar a coluna `CPF` (grid de clientes). Lógica de paginação/ordenação mantida. |
| `FI_SP_VerificaCliente` | **Nova** no repositório | `@CPF`, `@ID = 0`. Retorna uma linha se existir **outro** cliente (ID diferente) com o mesmo CPF. Usada na inclusão e na alteração. |
| `FI_SP_DelCliente` | **Corrigida** | O arquivo original criava a procedure com o nome errado (`FI_SP_ConsCliente`). Agora cria `FI_SP_DelCliente` e apaga os beneficiários do cliente antes do cliente. |
| `FI_SP_ConsBenef` | **Nova** no repositório | `@IDCLIENTE`. Lista os beneficiários do cliente (`ID, CPF, NOME, IDCLIENTE`). |

Todas usam `CREATE OR ALTER PROCEDURE`. Como a gravação de cliente + beneficiários é feita dentro da procedure, com `TRY/CATCH`, `SET XACT_ABORT ON` e `ROLLBACK` em caso de erro, **nunca fica um cliente gravado sem seus beneficiários** (ou o contrário).

### 2.4 Formato do CPF no banco
O CPF é armazenado **apenas com dígitos** (ex.: `52998224725`). A máscara `999.999.999-99` é aplicada somente na tela (e reaplicada ao carregar os dados). Isso garante comparação de duplicidade consistente, independente de como o usuário digitou.

### 2.5 Como a lista de beneficiários chega à procedure
O C# serializa a lista em XML e envia como um único parâmetro `@BENEFICIARIOS` (tipo `Xml`):

```xml
<Beneficiarios>
  <Beneficiario><CPF>52998224725</CPF><NOME>Ana</NOME></Beneficiario>
  <Beneficiario><CPF>11144477735</CPF><NOME>Bia</NOME></Beneficiario>
</Beneficiarios>
```

A procedure lê com `@BENEFICIARIOS.nodes('/Beneficiarios/Beneficiario')` e faz o `INSERT ... SELECT`. Valores especiais (`&`, `<`, aspas) são escapados pelo `XElement`.
Semântica: `NULL` = não mexer nos beneficiários; `<Beneficiarios />` (vazio) = remover todos.

---

## 3. Camadas C# (DML / DAL / BLL)

Projeto `FI.AtividadeEntrevista`.

### DML (entidades)
| Arquivo | Alteração |
|---|---|
| `DML\Beneficiario.cs` | **Novo.** Entidade com `Id`, `CPF`, `Nome`, `IdCliente`. |
| `DML\Cliente.cs` | + `CPF` e + `List<Beneficiario> Beneficiarios` (`null` = não alterar os beneficiários). |

### DAL (acesso a dados)
| Arquivo | Alteração |
|---|---|
| `DAL\Padrao\FI.AcessoDados.cs` | + `ExecutarScript(string sql)`, que executa um lote SQL em texto (usado apenas para aplicar a estrutura do banco). `Executar` e `Consultar` (procedures) permanecem como estavam. |
| `DAL\Padrao\FI.EstruturaBanco.cs` | **Novo.** Lê os `.sql` embutidos (estrutura primeiro, depois procedures), separa por `GO` e executa. Em caso de erro informa o script que falhou. |
| `DAL\Clientes\DaoCliente.cs` | `Incluir` e `Alterar` enviam `CPF` e `BENEFICIARIOS` (XML). `VerificarExistencia(CPF, idIgnorar)` agora recebe o ID a ignorar. `Converter` lê a coluna `CPF`. + método privado `ParametroBeneficiarios` que monta o XML. |
| `DAL\Beneficiarios\DaoBeneficiario.cs` | **Novo.** `Consultar(idCliente)` chama `FI_SP_ConsBenef`. |

### BLL (regras de negócio)
| Arquivo | Alteração |
|---|---|
| `BLL\ValidadorCpf.cs` | **Novo.** `Validar` (formato + dígitos verificadores + rejeita sequências repetidas como `111.111.111-11`), `SomenteDigitos` e `Formatar`. |
| `BLL\RegraNegocioException.cs` | **Nova.** Exceção com mensagem exibível ao usuário. |
| `BLL\BoBancoDados.cs` | **Novo.** `GarantirEstrutura()` — ponto de entrada chamado no `Application_Start`. |
| `BLL\BoCliente.cs` | `Consultar` também carrega os beneficiários. `VerificarExistencia(CPF, idIgnorar)`. `Incluir`/`Alterar` convertem violação de índice único do SQL Server (erros 2601/2627, ex.: duas pessoas gravando o mesmo CPF ao mesmo tempo) em `RegraNegocioException` com mensagem amigável. |

### Projeto
`FI.AtividadeEntrevista.csproj`: novos arquivos `.cs` incluídos; os `.sql` passaram de `Content` para **`EmbeddedResource`**.

---

## 4. Camada web

Projeto `FI.WebAtividadeEntrevista`.

### Controller e Models
| Arquivo | Alteração |
|---|---|
| `Models\ClienteModel.cs` | + `CPF` (`[Required]`, mensagem em português) e + `List<BeneficiarioModel> Beneficiarios`. |
| `Models\BeneficiarioModel.cs` | **Novo.** `Id`, `CPF` (`[Required]`), `Nome` (`[Required]`, `[MaxLength(50)]`). |
| `Controllers\ClienteController.cs` | `Incluir` e `Alterar` (POST) passam a: validar CPF do cliente (formato, dígito, duplicidade excluindo o próprio cliente) e dos beneficiários (formato, dígito, duplicidade na lista); mapear o model para `DML.Cliente` normalizando os CPFs para dígitos; tratar `RegraNegocioException`. `Alterar` (GET) devolve CPF formatado e beneficiários. Métodos auxiliares: `ValidarCliente`, `MapearParaCliente`, `RetornarErros`. |
| `Global.asax.cs` | `Application_Start` chama `BoBancoDados().GarantirEstrutura()`. |

### Views
| Arquivo | Alteração |
|---|---|
| `Views\Cliente\Forms.cshtml` | Linhas reorganizadas conforme o PDF (*Nome \| Sobrenome*, *CPF \| Nacionalidade*). Campo **CPF** (`required`, `maxlength=14`, `data-mask="cpf"`). Botão **Beneficiários** à esquerda de *Salvar/Voltar*. **Modal** de beneficiários (fora do `<form>`): campos CPF e Nome, botão *Incluir*, grid com colunas *CPF / Nome / Ações* e área de mensagens de erro. Reutilizado por *Incluir* e *Alterar*. |
| `Views\Cliente\Alterar.cshtml` | O JSON do modelo injetado na página passa a escapar `<` (`\u003c`), para que nomes contendo `</script>` não quebrem o script. |

### Scripts
| Arquivo | Alteração |
|---|---|
| `Scripts\Clientes\FI.Cpf.js` | **Novo.** Máscara automática em todo campo `data-mask="cpf"`, `validar`, `formatar`, `somenteDigitos`. Mesmo algoritmo do `ValidadorCpf.cs`. |
| `Scripts\Clientes\FI.Beneficiarios.js` | **Novo.** Lógica do pop-up: incluir, alterar (o botão *Incluir* vira *Salvar* durante a edição), excluir; valida CPF, nome e duplicidade; renderiza a grid com `.text()` (sem injeção de HTML); expõe `carregar`, `obter` e `limpar`. |
| `Scripts\Clientes\FI.Clientes.js` | Valida o CPF antes de enviar; envia o formulário como **JSON** incluindo `CPF` e `Beneficiarios`; limpa a lista de beneficiários após o sucesso. `ModalDialog` passou a escapar HTML e converter quebras de linha (mensagens do servidor têm várias linhas). |
| `Scripts\Clientes\FI.AltClientes.js` | Preenche o CPF e carrega os beneficiários existentes; envia `Id` explicitamente junto com o JSON; mesmas melhorias de validação/`ModalDialog`. |
| `App_Start\BundleConfig.cs` | Os bundles `~/bundles/clientes` e `~/bundles/altClientes` passam a incluir `FI.Cpf.js` e `FI.Beneficiarios.js`. |
| `FI.WebAtividadeEntrevista.csproj` | Novos arquivos (`BeneficiarioModel.cs`, `FI.Cpf.js`, `FI.Beneficiarios.js`) incluídos no projeto. |

---

## 5. Fluxos de gravação

### Incluir cliente
```
[Salvar] ──> FI.Clientes.js
               ├─ valida CPF (FI.Cpf.validar)  ── inválido ──> mensagem, não envia
               └─ POST /Cliente/Incluir (JSON: dados + CPF + Beneficiarios[])
                    └─ ClienteController.Incluir
                         ├─ ValidarCliente: [Required], CPF válido, CPF já existe?, beneficiários válidos/sem repetição
                         ├─ erro  ──> HTTP 400 com as mensagens (uma por linha)
                         └─ BoCliente.Incluir ──> DaoCliente ──> FI_SP_IncClienteV2 (TRANSAÇÃO: cliente + beneficiários)
```

### Alterar cliente
```
[Salvar] ──> FI.AltClientes.js ──> POST /Cliente/Alterar (JSON com Id)
                    └─ ClienteController.Alterar
                         ├─ ValidarCliente (duplicidade de CPF ignora o próprio cliente: FI_SP_VerificaCliente @ID)
                         └─ BoCliente.Alterar ──> DaoCliente ──> FI_SP_AltCliente
                                                   (TRANSAÇÃO: update do cliente + substitui a lista de beneficiários)
```

Payload enviado pelo navegador (exemplo):
```json
{
  "Id": 3,
  "NOME": "João", "Sobrenome": "da Silva", "Nacionalidade": "brasileira",
  "CEP": "01011-100", "Estado": "SP", "Cidade": "São Paulo",
  "Logradouro": "Rua Boa Vista 253", "Email": "joao@email.com", "Telefone": "(11) 2020-3030",
  "CPF": "529.982.247-25",
  "Beneficiarios": [
    { "Id": 0, "CPF": "111.444.777-35", "Nome": "Maria" }
  ]
}
```

### Abrir a tela de alteração
`ClienteController.Alterar (GET)` → `BoCliente.Consultar` (cliente via `FI_SP_ConsCliente` + beneficiários via `FI_SP_ConsBenef`) → CPFs formatados → JSON na página (`obj`) → `FI.AltClientes.js` preenche o formulário e chama `FI.Beneficiarios.carregar`.

---

## 6. Regras de validação

Cada regra é verificada em mais de uma camada (a tela dá resposta rápida; o servidor e o banco garantem a integridade).

| Regra | Tela (JS) | Servidor | Banco |
|---|---|---|---|
| CPF do cliente obrigatório | `required` + JS | `[Required]` | coluna aceita `NULL` apenas para clientes antigos |
| CPF válido (formato + dígitos verificadores) | `FI.Cpf.validar` | `ValidadorCpf.Validar` | — |
| CPF de cliente não duplicado | — | `FI_SP_VerificaCliente` | `UX_CLIENTES_CPF` |
| CPF e nome do beneficiário obrigatórios; nome até 50 caracteres | pop-up | `BeneficiarioModel` | `NOT NULL`, `VARCHAR(50)` |
| CPF do beneficiário válido | pop-up | `ValidarCliente` | — |
| Sem CPF repetido entre beneficiários do mesmo cliente | pop-up | `ValidarCliente` (`HashSet`) | `UX_BENEFICIARIOS_IDCLIENTE_CPF` |

Mensagens exibidas ao usuário:
- `O CPF informado é inválido.`
- `Já existe um cliente cadastrado com o CPF informado.`
- `O CPF do beneficiário <cpf> é inválido.` / `O CPF <cpf> está duplicado na lista de beneficiários.`
- No pop-up: `Informe o CPF do beneficiário.`, `Informe o nome do beneficiário.`, `Já existe um beneficiário com este CPF para este cliente.`
- `Já existe um registro com o CPF informado.` (corrida entre duas gravações simultâneas; barrada pelo índice único)

---

## 7. Arquivos novos e alterados

### Novos
```
README.md
DOCUMENTACAO_ALTERACOES.md
Banco\Script_Banco_Completo.sql
FI.AtividadeEntrevista\BLL\BoBancoDados.cs
FI.AtividadeEntrevista\BLL\RegraNegocioException.cs
FI.AtividadeEntrevista\BLL\ValidadorCpf.cs
FI.AtividadeEntrevista\DAL\Beneficiarios\DaoBeneficiario.cs
FI.AtividadeEntrevista\DAL\Beneficiarios\Procedures\FI_SP_ConsBenef.sql
FI.AtividadeEntrevista\DAL\Clientes\Procedures\FI_SP_VerificaCliente.sql
FI.AtividadeEntrevista\DAL\Padrao\FI.EstruturaBanco.cs
FI.AtividadeEntrevista\DAL\Scripts\Estrutura.sql
FI.AtividadeEntrevista\DML\Beneficiario.cs
FI.WebAtividadeEntrevista\Models\BeneficiarioModel.cs
FI.WebAtividadeEntrevista\Scripts\Clientes\FI.Beneficiarios.js
FI.WebAtividadeEntrevista\Scripts\Clientes\FI.Cpf.js
```

### Alterados
```
FI.AtividadeEntrevista\BLL\BoCliente.cs
FI.AtividadeEntrevista\DAL\Clientes\DaoCliente.cs
FI.AtividadeEntrevista\DAL\Clientes\Procedures\FI_SP_AltCliente.sql
FI.AtividadeEntrevista\DAL\Clientes\Procedures\FI_SP_ConsCliente.sql
FI.AtividadeEntrevista\DAL\Clientes\Procedures\FI_SP_DelCliente.sql
FI.AtividadeEntrevista\DAL\Clientes\Procedures\FI_SP_IncCliente.sql
FI.AtividadeEntrevista\DAL\Clientes\Procedures\FI_SP_PesqCliente.sql
FI.AtividadeEntrevista\DAL\Padrao\FI.AcessoDados.cs
FI.AtividadeEntrevista\DML\Cliente.cs
FI.AtividadeEntrevista\FI.AtividadeEntrevista.csproj
FI.WebAtividadeEntrevista\App_Start\BundleConfig.cs
FI.WebAtividadeEntrevista\Controllers\ClienteController.cs
FI.WebAtividadeEntrevista\FI.WebAtividadeEntrevista.csproj
FI.WebAtividadeEntrevista\Global.asax.cs
FI.WebAtividadeEntrevista\Models\ClienteModel.cs
FI.WebAtividadeEntrevista\Scripts\Clientes\FI.AltClientes.js
FI.WebAtividadeEntrevista\Scripts\Clientes\FI.Clientes.js
FI.WebAtividadeEntrevista\Views\Cliente\Alterar.cshtml
FI.WebAtividadeEntrevista\Views\Cliente\Forms.cshtml
```

Não houve alteração em `Web.config` (a connection string continua a mesma), nas rotas, no layout, na listagem de clientes nem nos pacotes NuGet.

---

## 8. Decisões de projeto e pontos de atenção

- **Banco atualizado pela própria aplicação.** O `.mdf` é binário e o enunciado exige a criação do campo no banco; por isso a estrutura é aplicada por script idempotente na inicialização, em vez de depender de o avaliador rodar algo manualmente. O script manual (`Banco\Script_Banco_Completo.sql`) existe como plano B.
- **CPF só com dígitos no banco**, máscara apenas visual (comparações de duplicidade consistentes).
- **`CLIENTES.CPF` aceita `NULL`** no banco para não quebrar clientes que já existissem sem CPF; a obrigatoriedade é garantida pela aplicação (tela e servidor), e o índice único é *filtrado* (`WHERE CPF IS NOT NULL`) para permitir vários clientes antigos sem CPF.
- **Alteração de beneficiários = "substituir a lista".** Ao salvar a alteração do cliente, os beneficiários são apagados e reinseridos conforme a grid. Isso é atômico (mesma transação do cliente) e evita conflitos de índice único ao trocar CPFs entre beneficiários; como contrapartida, os `ID`s dos beneficiários são regerados a cada alteração do cliente (nenhuma outra parte do sistema depende desses IDs).
- **Beneficiários só são gravados em "Salvar"**, como pede o enunciado: o pop-up trabalha apenas em memória até lá.
- **Segurança básica:** consultas somente via parâmetros de procedure (sem concatenação de SQL com dados do usuário), grid renderizada com `.text()` e mensagens de erro com HTML escapado.
- **Nome do beneficiário:** limitado a 50 caracteres (mesmo limite do nome do cliente).
- **Fora do escopo (mantido como estava):** listagem de clientes sem coluna de CPF, lista de estados com apenas SP/PE, exclusão de cliente sem botão na tela (a procedure `FI_SP_DelCliente` foi apenas corrigida).
