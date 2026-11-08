# FI.WebAtividadeEntrevista — Teste Prático

Sistema de manutenção de clientes (ASP.NET MVC 5 / .NET Framework 4.8 / SQL Server LocalDB), com as duas implementações pedidas no teste:

1. **CPF do cliente** — campo obrigatório, com máscara `999.999.999-99`, validação do dígito verificador e bloqueio de CPF duplicado.
2. **Botão "Beneficiários"** — pop-up para cadastrar, alterar e excluir beneficiários (CPF + Nome) do cliente, gravados quando o cliente é salvo.

> Lista completa e detalhada de tudo o que foi alterado: [`DOCUMENTACAO_ALTERACOES.md`](DOCUMENTACAO_ALTERACOES.md)

---

## 1. Pré-requisitos

Visual Studio 2022 (a edição *Community* serve) com os itens abaixo instalados — são os mesmos citados no enunciado do teste:

| Onde marcar no *Visual Studio Installer* | Item |
|---|---|
| Cargas de trabalho | **ASP.NET e desenvolvimento Web** |
| Componentes individuais | **Pacote de direcionamento do .NET Framework 4.8** |
| Componentes individuais | **SDK do .NET Framework 4.8** |
| Componentes individuais | **SQL Server Express 2019 LocalDB** |

Também é necessário **acesso à internet na primeira compilação**, para o NuGet baixar os pacotes (a pasta `packages` não vai no ZIP).

---

## 2. Como executar (passo a passo)

1. **Descompacte** o ZIP em uma pasta simples, de preferência fora de pastas sincronizadas/protegidas.
   Exemplo: `C:\Projetos\FI.WebAtividadeEntrevista\`
   (evite `OneDrive`, `Desktop`/`Documentos` sincronizados e caminhos muito longos; veja a seção 5).
2. Abra o arquivo **`FI.WebAtividadeEntrevista.sln`** no Visual Studio 2022.
3. **Restaure os pacotes NuGet:** clique com o botão direito na **Solution** → **Restaurar Pacotes NuGet**. Aguarde terminar.
4. Menu **Compilar → Recompilar Solução** (`Build → Rebuild Solution`).
   Esse passo é importante: é ele que gera `bin\roslyn\csc.exe` (veja o erro 1 da seção 5).
5. Confirme que o projeto de inicialização é **`FI.WebAtividadeEntrevista`** (negrito no *Gerenciador de Soluções*; se não estiver: botão direito no projeto → *Definir como Projeto de Inicialização*).
6. Pressione **F5** (ou `Ctrl+F5`). O navegador abre em `https://localhost:44333/`.
   Se o Visual Studio pedir para confiar no certificado SSL do IIS Express, clique em **Sim**.
7. No menu superior clique em **Clientes**.

> A **primeira** execução pode levar alguns segundos a mais: é quando o LocalDB anexa o banco e a aplicação aplica a estrutura (seção 3). Nas seguintes é imediato.

---

## 3. Banco de dados

### Como funciona
- O banco é o arquivo `FI.WebAtividadeEntrevista\App_Data\BancoDeDados.mdf` (SQL Server **LocalDB**), anexado automaticamente pela connection string do `Web.config`:
  ```
  Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\BancoDeDados.mdf;Integrated Security=True
  ```
- **Não é preciso rodar nenhum script manualmente.** Ao subir a aplicação (`Global.asax.cs → Application_Start`), o método `BoBancoDados.GarantirEstrutura()` executa os scripts `.sql` embutidos na DLL `FI.AtividadeEntrevista`, que:
  - criam a coluna `CLIENTES.CPF`;
  - criam a tabela `BENEFICIARIOS` (`ID`, `CPF`, `NOME`, `IDCLIENTE`);
  - criam os índices únicos que impedem CPF duplicado;
  - criam/atualizam as stored procedures (`CREATE OR ALTER`).
- Os scripts são **idempotentes**: podem rodar a cada execução sem erro e **sem apagar dados**.
- Toda a gravação/consulta da aplicação é feita via **stored procedures**.

### Estrutura criada

```
CLIENTES        (já existia)  + CPF VARCHAR(14) NULL   — índice único filtrado UX_CLIENTES_CPF (CPF IS NOT NULL)
BENEFICIARIOS   (nova)        ID BIGINT IDENTITY PK, CPF VARCHAR(14), NOME VARCHAR(50),
                              IDCLIENTE BIGINT FK -> CLIENTES(ID) ON DELETE CASCADE
                              índice único UX_BENEFICIARIOS_IDCLIENTE_CPF (IDCLIENTE, CPF)
```

O CPF é gravado **somente com dígitos** (ex.: `52998224725`); a máscara é apenas visual.

### Aplicar o banco manualmente (opcional / plano B)
Só necessário se a aplicação exibir a mensagem *"Falha ao atualizar a estrutura do banco de dados…"*. O arquivo **`Banco\Script_Banco_Completo.sql`** contém tudo (estrutura + procedures), na ordem correta.

1. **Pare** a aplicação (Shift+F5).
2. No Visual Studio: **Exibir → Gerenciador de Servidores** → *Conexões de Dados* → **Adicionar Conexão…**
3. Fonte de dados: **Arquivo de Banco de Dados do Microsoft SQL Server** → *Procurar* → selecione `FI.WebAtividadeEntrevista\App_Data\BancoDeDados.mdf` → *Autenticação do Windows* → **OK**.
4. Botão direito na conexão criada → **Nova Consulta** → abra/cole o conteúdo de `Banco\Script_Banco_Completo.sql` → **Executar** (`Ctrl+Shift+E`).
5. Botão direito na conexão → **Fechar Conexão** (e, se existir, *Excluir*), para liberar o arquivo `.mdf`.
6. Rode a aplicação novamente.

### Como conferir se o banco está correto
Em uma Nova Consulta na conexão acima:

```sql
SELECT COL_LENGTH('dbo.CLIENTES', 'CPF') AS ColunaCpf;           -- deve retornar 14
SELECT name FROM sys.tables WHERE name IN ('CLIENTES', 'BENEFICIARIOS');   -- 2 linhas
SELECT name FROM sys.indexes WHERE name LIKE 'UX[_]%';           -- UX_CLIENTES_CPF e UX_BENEFICIARIOS_IDCLIENTE_CPF
SELECT name FROM sys.procedures WHERE name LIKE 'FI[_]SP[_]%' ORDER BY name;
```

Para visualizar os dados: botão direito na tabela (`CLIENTES` / `BENEFICIARIOS`) → *Mostrar Dados da Tabela*.

---

## 4. Roteiro rápido de testes

CPFs **válidos** para teste: `111.444.777-35`, `529.982.247-25`, `123.456.789-09`, `000.000.001-91`, `000.000.002-72`
CPFs **inválidos**: `111.111.111-11`, `123.456.789-10`

| # | Ação | Resultado esperado |
|---|---|---|
| 1 | Clientes → **Novo Cliente**, deixar o CPF em branco e salvar | Navegador exige o preenchimento (campo obrigatório) |
| 2 | Digitar `52998224725` no CPF | Vira `529.982.247-25` automaticamente |
| 3 | Preencher tudo com CPF `111.111.111-11` e salvar | Mensagem "O CPF informado é inválido." |
| 4 | Preencher tudo com CPF `529.982.247-25` e salvar | "Cadastro efetuado com sucesso" |
| 5 | Novo cliente com o mesmo CPF `529.982.247-25` | "Já existe um cliente cadastrado com o CPF informado." |
| 6 | Em **Novo Cliente**, clicar em **Beneficiários** | Abre o pop-up com a grid vazia |
| 7 | No pop-up, incluir CPF inválido | Mensagem de erro dentro do pop-up |
| 8 | Incluir `111.444.777-35` + nome; depois tentar o mesmo CPF de novo | 1º entra na grid; 2º é barrado ("Já existe um beneficiário com este CPF…") |
| 9 | **Alterar** um beneficiário da grid (botão *Alterar* → editar → *Salvar*) | Linha atualizada |
| 10 | **Excluir** um beneficiário da grid | Linha removida |
| 11 | Fechar o pop-up e clicar **Salvar** no cliente | Cliente e beneficiários gravados juntos |
| 12 | Clientes → **Alterar** o cliente salvo → **Beneficiários** | Beneficiários gravados aparecem na grid |
| 13 | Alterar o cliente mantendo o próprio CPF e salvar | Salva normalmente (não acusa duplicidade com ele mesmo) |
| 14 | Na alteração, excluir/alterar beneficiários e salvar; reabrir | Lista reflete exatamente o que foi salvo |

---

## 5. Solução de problemas

| Erro / sintoma | Causa | Solução |
|---|---|---|
| **1.** `Não foi possível localizar uma parte do caminho '...\bin\roslyn\csc.exe'` | Os pacotes NuGet não foram restaurados / o projeto ainda não foi compilado. A pasta `bin\roslyn` é criada no build. | Botão direito na Solution → *Restaurar Pacotes NuGet* → *Compilar → Recompilar Solução*. Se persistir: apague as pastas `bin` e `obj` dos dois projetos e recompile. Se ainda faltar, no *Console do Gerenciador de Pacotes*: `Update-Package -reinstall Microsoft.CodeDom.Providers.DotNetCompilerPlatform -ProjectName FI.WebAtividadeEntrevista` |
| **2.** `Could not load file or assembly ...` ou erros de pacote ausente ao compilar | Restauração NuGet incompleta (sem internet, ou fonte `nuget.org` desativada) | *Ferramentas → Opções → Gerenciador de Pacotes NuGet → Fontes de Pacotes*: marque `nuget.org`. Restaure e recompile. |
| **3.** `A network-related or instance-specific error ...` / `Local Database Runtime error occurred. Cannot create an automatic instance` | **LocalDB não está instalado** ou a instância não inicia | Instale *SQL Server Express 2019 LocalDB* pelo Visual Studio Installer. Confira no Prompt de Comando: `sqllocaldb info` (deve listar `MSSQLLocalDB`) e `sqllocaldb start MSSQLLocalDB`. |
| **4.** `Cannot attach the file '...BancoDeDados.mdf' as database` / `Unable to open the physical file ... Operating system error 5: Access is denied` | Permissão na pasta, arquivo bloqueado/somente leitura, ou ZIP baixado e "bloqueado" pelo Windows | Mova o projeto para uma pasta simples (ex.: `C:\Projetos\...`). Em `App_Data`, clique com o botão direito em `BancoDeDados.mdf` e `BancoDeDados_log.ldf` → *Propriedades* → desmarque **Somente leitura** (e marque **Desbloquear**, se aparecer). Se o ZIP original estiver bloqueado: *Propriedades do ZIP → Desbloquear* e extraia de novo. |
| **5.** `Cannot attach the file ... because it is already attached` ou o arquivo está em uso | Outra instância do VS/IIS Express ou uma conexão de dados aberta está usando o `.mdf` | Feche o Visual Studio, execute `sqllocaldb stop MSSQLLocalDB` e abra de novo. No VS, feche *Conexões de Dados* que apontem para o `.mdf`. |
| **6.** `... is version NNN, and this server supports version MMM` | O `.mdf` foi aberto antes numa versão de SQL **mais nova** que a do computador atual | Use o `.mdf` original do ZIP (sem ter sido aberto por LocalDB mais novo) ou instale o *SQL Server Express 2019 LocalDB*, como pede o enunciado. |
| **7.** Página amarela com `Falha ao atualizar a estrutura do banco de dados (script '...')` | Algum lote do script de estrutura falhou (a mensagem informa qual script e o motivo) | Aplique o banco manualmente (seção 3 → *Aplicar o banco manualmente*) e rode de novo. Se o erro persistir, a mensagem completa indica o motivo exato. |
| **8.** A tela abre mas o botão **Beneficiários**/máscara de CPF não funciona | Cache do navegador com os scripts antigos | `Ctrl+F5` na página. |
| **9.** Aviso de certificado ao abrir `https://localhost:44333/` | Certificado de desenvolvimento do IIS Express não confiado | Aceite a confiança do certificado quando o Visual Studio perguntar (ou continue pelo navegador). É apenas ambiente local. |

Último recurso para problemas persistentes de LocalDB (apenas se nada acima resolver): `sqllocaldb stop MSSQLLocalDB` e `sqllocaldb delete MSSQLLocalDB`; na execução seguinte a instância é recriada automaticamente. *(Remove o registro da instância; os arquivos `.mdf` não são apagados.)*

---

## 6. Estrutura da solução

```
FI.WebAtividadeEntrevista.sln
├── FI.WebAtividadeEntrevista      Camada web (MVC): Controllers, Models, Views, Scripts, App_Data (.mdf)
├── FI.AtividadeEntrevista         Regras e dados
│   ├── DML   entidades (Cliente, Beneficiario)
│   ├── BLL   regras de negócio (BoCliente, ValidadorCpf, BoBancoDados)
│   └── DAL   acesso a dados via stored procedures + scripts SQL embutidos
├── Banco\Script_Banco_Completo.sql   script único para aplicar o banco manualmente (opcional)
├── README.md                         este arquivo
└── DOCUMENTACAO_ALTERACOES.md        detalhamento de todas as alterações
```
