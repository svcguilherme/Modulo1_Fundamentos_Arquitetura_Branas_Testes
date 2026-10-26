# Testes Automatizados com xUnit (.NET)

## 1. O que é um teste automatizado

Um **teste automatizado** é um código que executa outro código e **verifica se o resultado é o esperado**, sem intervenção humana. Em vez de abrir a aplicação, preencher um formulário e conferir na tela se a conta foi criada, você escreve um programa que faz isso e pode ser executado milhares de vezes, em segundos, com `dotnet test`.

Por que importa:

- **Feedback rápido**: você descobre na hora se quebrou algo.
- **Segurança para refatorar**: pode mudar a estrutura interna do código sabendo que o comportamento continua o mesmo.
- **Documentação viva**: o teste descreve como o sistema deve se comportar ("Deve criar uma conta").
- **Base para CI/CD**: o pipeline só publica se os testes passarem.

### Tipos mais comuns

| Tipo | O que testa | Velocidade | Exemplo |
|---|---|---|---|
| **Unidade** | Uma classe/função isolada, sem I/O | Muito rápido | Validar CPF |
| **Integração** | Componentes juntos (serviço + banco, por exemplo) | Médio | `signup` gravando no banco real |
| **E2E (ponta a ponta)** | O sistema inteiro, pela borda (API/UI) | Lento | Chamar `POST /signup` via HTTP |

A ideia da **pirâmide de testes**: muitos testes de unidade na base, menos de integração no meio e poucos E2E no topo.

---

## 2. A disciplina: estrutura de um teste

Um bom teste segue sempre a mesma estrutura em três partes. O exemplo da aula usa **Given / When / Then** (vindo do BDD); no mundo .NET é muito comum chamar de **Arrange / Act / Assert (AAA)**. É a mesma coisa:

| BDD | AAA | Significado |
|---|---|---|
| **Given** (Dado) | **Arrange** (Preparar) | Define **todos os parâmetros da request** (o `input`) e monta o cenário (objetos, dependências) |
| **When** (Quando) | **Act** (Agir) | Executa **uma** ação, enviando a request, e recebe a **saída** (`output`) |
| **Then** (Então) | **Assert** (Verificar) | Confere se a saída, ou seja, **o payload que volta**, é o esperado |

No exemplo da aula:

- **Given/Arrange:** o objeto `input` com `name`, `email`, `document` e `password` é a request completa.
- **When/Act:** `accountService.signup(input)` envia a request.
- **Then/Assert:** o `output` é o payload de resposta, e é sobre ele (e sobre a conta consultada com `getAccount`) que as asserções são feitas.

```
Request (input)  ──►  Ação (signup)  ──►  Response/payload (output)
   Given/Arrange         When/Act             Then/Assert
```

Boas práticas da disciplina:

- **Um comportamento por teste**: o nome do teste diz exatamente o que se espera ("Deve criar uma conta", "Não deve criar conta com e-mail inválido").
- **Testes independentes**: um teste não depende de outro ter rodado antes, e a ordem de execução não importa.
- **Determinísticos**: o mesmo código dá sempre o mesmo resultado (cuidado com data/hora, aleatoriedade e dados compartilhados no banco).
- **Rápidos e legíveis**: teste também é código de produção e merece o mesmo cuidado.
- **Teste o comportamento, não a implementação**: verifique o que o sistema faz (a conta foi criada com os dados certos), não como ele faz por dentro.
- **TDD (Test-Driven Development)**: ciclo **Red → Green → Refactor**. Escreve o teste primeiro (falha, *red*), implementa o mínimo para passar (*green*) e depois melhora o código (*refactor*).

---

## 3. Estrutura do xUnit

A estrutura do xUnit tem três níveis: **projeto → classe → método**.

### 3.1 Projeto de testes

```
ProjetoTestes.slnx
├── ProjetoTestes.Api/                  ← código de produção
└── ProjetoTestes.Tests/                ← projeto de testes (xUnit)
    ├── ProjetoTestes.Tests.csproj      ← pacotes xunit, runner, Test.Sdk
    ├── Unit/
    │   └── AccountServiceTests.cs
    └── Integration/
        └── SignupApiTests.cs
```

- O projeto de testes é separado do código de produção e o referencia com `dotnet add reference`.
- Por convenção, ele leva o sufixo `.Tests` e espelha as pastas do projeto testado.

### 3.2 Classe de teste (equivale ao `describe` do Jest)

```csharp
public class AccountServiceTests            // agrupa os testes de AccountService
{
    private readonly AccountService _sut;   // sut = "system under test"

    public AccountServiceTests()            // ~ beforeEach
    {
        _sut = new AccountService();
    }
}
```

- Não precisa de atributo na classe. Basta ser `public`.
- **O xUnit cria uma nova instância da classe para cada teste.** Por isso o construtor funciona como `beforeEach` e os testes não compartilham estado.
- Por padrão, os testes de uma mesma classe rodam em sequência, e classes diferentes rodam em paralelo.

### 3.3 Método de teste (equivale ao `test`/`it` do Jest)

```csharp
[Fact(DisplayName = "Deve criar uma conta")]
public async Task Deve_criar_uma_conta()
{
    // Given / Arrange: parâmetros da request
    var input = new SignupInput("John Doe", "john.doe@gmail.com", "97456321558", "asdQWE123");

    // When / Act: envia a request e recebe a saída
    var output = await _sut.Signup(input);

    // Then / Assert: verifica o payload de retorno
    Assert.NotEqual(Guid.Empty, output.AccountId);
}
```

| Atributo | Uso |
|---|---|
| `[Fact]` | Teste sem parâmetros, que roda uma vez |
| `[Theory]` + `[InlineData(...)]` | Mesmo teste com vários conjuntos de dados (equivale a `test.each`) |

### 3.4 Ciclo de vida

| Escopo | Mecanismo | Equivalente no Jest |
|---|---|---|
| Por teste | Construtor + `IDisposable` / `IAsyncLifetime` | `beforeEach` / `afterEach` |
| Por classe | `IClassFixture<T>` | `beforeAll` / `afterAll` |
| Entre várias classes | `[Collection]` + `ICollectionFixture<T>` | setup global |

Exemplo de fixture compartilhada por classe, útil para criar uma conexão de banco uma única vez:

```csharp
public class DatabaseFixture : IDisposable
{
    public DbConnection Connection { get; } = /* abre conexão */;
    public void Dispose() => Connection.Dispose();
}

public class AccountRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _db;
    public AccountRepositoryTests(DatabaseFixture db) => _db = db;  // o xUnit injeta a fixture
}
```

### 3.5 Resumo

```
Solução
└── Projeto de testes (.Tests)
    └── Classe de teste   → agrupa (describe), construtor = beforeEach
        └── Método [Fact] / [Theory]   → um teste (Given / When / Then)
```

---

## 4. Do Jest (JavaScript/TypeScript) para o xUnit (C#)

### O teste da aula em JavaScript (Jest/Vitest)

```js
test("Deve criar uma conta", async () => {
  // Given
  const input = {
    name: "John Doe",
    email: "john.doe@gmail.com",
    document: "97456321558",
    password: "asdQWE123"
  };
  // When
  const output = await accountService.signup(input);
  // Then
  expect(output.accountId).toBeDefined();
  const account = await accountService.getAccount(output.accountId);
  expect(account.name).toBe(input.name);
  expect(account.email).toBe(input.email);
  expect(account.document).toBe(input.document);
});
```

### O mesmo teste em C# com xUnit

```csharp
public class AccountServiceTests
{
    private readonly AccountService _accountService = new();

    [Fact(DisplayName = "Deve criar uma conta")]
    public async Task Deve_criar_uma_conta()
    {
        // Given
        var input = new SignupInput(
            Name: "John Doe",
            Email: "john.doe@gmail.com",
            Document: "97456321558",
            Password: "asdQWE123");

        // When
        var output = await _accountService.Signup(input);

        // Then
        Assert.NotEqual(Guid.Empty, output.AccountId);
        var account = await _accountService.GetAccount(output.AccountId);
        Assert.NotNull(account);
        Assert.Equal(input.Name, account.Name);
        Assert.Equal(input.Email, account.Email);
        Assert.Equal(input.Document, account.Document);
    }
}

// Tipos de entrada/saída (em C# os dados precisam de um tipo)
public record SignupInput(string Name, string Email, string Document, string Password);
public record SignupOutput(Guid AccountId);
```

### Equivalência linha a linha

| Jest | xUnit | Observação |
|---|---|---|
| `test("nome", async () => { ... })` | `[Fact] public async Task Nome() { ... }` | Cada teste é um **método** com o atributo `[Fact]`. Async retorna `Task` (nunca `async void`). |
| Nome do teste em texto livre | Nome do método ou `[Fact(DisplayName = "Deve criar uma conta")]` | `DisplayName` permite nome com espaços e acentos. |
| `const input = { ... }` | `var input = new SignupInput(...)` | C# é tipado: usa-se `record`, classe ou tipo anônimo (`new { Name = "..." }`). |
| `await x()` | `await x()` | Igual. |
| `expect(x).toBeDefined()` | `Assert.NotNull(x)` | Para `Guid`, que nunca é `null`, use `Assert.NotEqual(Guid.Empty, x)`. |
| `expect(actual).toBe(expected)` | `Assert.Equal(expected, actual)` | **Atenção à ordem: no xUnit o esperado vem primeiro.** Se inverter, a mensagem de erro fica trocada. |

> No Jest você escreve `expect(atual).toBe(esperado)`. No xUnit é `Assert.Equal(esperado, atual)`.

---

## 5. Tabela de referência: outros comandos comuns

### Asserções

| Jest | xUnit |
|---|---|
| `expect(x).toBe(y)` | `Assert.Equal(y, x)` |
| `expect(x).not.toBe(y)` | `Assert.NotEqual(y, x)` |
| `expect(obj).toEqual(outroObj)` (comparação estrutural) | `Assert.Equivalent(outroObj, obj)` (com `record`, `Assert.Equal` já compara por valor) |
| `expect(x).toBeDefined()` / `not.toBeNull()` | `Assert.NotNull(x)` |
| `expect(x).toBeNull()` / `toBeUndefined()` | `Assert.Null(x)` |
| `expect(x).toBeTruthy()` / `toBe(true)` | `Assert.True(x)` |
| `expect(x).toBeFalsy()` / `toBe(false)` | `Assert.False(x)` |
| `expect(lista).toContain(item)` | `Assert.Contains(item, lista)` |
| `expect(lista).not.toContain(item)` | `Assert.DoesNotContain(item, lista)` |
| `expect(lista).toHaveLength(3)` | `Assert.Equal(3, lista.Count)` |
| `expect(lista).toHaveLength(0)` | `Assert.Empty(lista)` |
| `expect(lista).toHaveLength(1)` | `Assert.Single(lista)` (retorna o item) |
| `expect(texto).toMatch(/regex/)` | `Assert.Matches("regex", texto)` |
| `expect(texto).toContain("abc")` | `Assert.Contains("abc", texto)` |
| `expect(x).toBeGreaterThan(5)` | `Assert.True(x > 5)` ou `Assert.InRange(x, 6, int.MaxValue)` |
| `expect(x).toBeInstanceOf(Tipo)` | `Assert.IsType<Tipo>(x)` |
| `expect(() => fn()).toThrow()` | `Assert.Throws<Exception>(() => fn())` |
| `await expect(fn()).rejects.toThrow("msg")` | `var ex = await Assert.ThrowsAsync<Exception>(() => fn()); Assert.Equal("msg", ex.Message);` |

Exemplo de teste de erro:

```csharp
[Fact(DisplayName = "Não deve criar conta com e-mail inválido")]
public async Task Nao_deve_criar_conta_com_email_invalido()
{
    var input = new SignupInput("John Doe", "john.doe", "97456321558", "asdQWE123");

    var ex = await Assert.ThrowsAsync<ArgumentException>(() => _accountService.Signup(input));

    Assert.Equal("Invalid email", ex.Message);
}
```

### Organização e ciclo de vida

| Jest | xUnit |
|---|---|
| `describe("AccountService", () => { ... })` | Uma **classe** de teste: `public class AccountServiceTests` |
| `beforeEach(() => { ... })` | **Construtor** da classe (o xUnit cria uma instância nova da classe para cada teste) |
| `afterEach(() => { ... })` | Implementar `IDisposable.Dispose()` (ou `IAsyncLifetime.DisposeAsync()` se for assíncrono) |
| `beforeEach(async () => ...)` | `IAsyncLifetime.InitializeAsync()` |
| `beforeAll` / `afterAll` | **Class Fixture**: `IClassFixture<MinhaFixture>` (compartilhado entre os testes da classe) |
| `test.skip(...)` | `[Fact(Skip = "motivo")]` |
| `test.each([...])("...", (a, b) => ...)` | `[Theory]` + `[InlineData(...)]` |

Exemplo de `[Theory]`, o equivalente ao `test.each`, para rodar o mesmo teste com vários dados:

```csharp
[Theory(DisplayName = "Não deve criar conta com nome inválido")]
[InlineData("")]
[InlineData("John")]
[InlineData("John123 Doe")]
public async Task Nao_deve_criar_conta_com_nome_invalido(string name)
{
    var input = new SignupInput(name, "john.doe@gmail.com", "97456321558", "asdQWE123");

    await Assert.ThrowsAsync<ArgumentException>(() => _accountService.Signup(input));
}
```

Exemplo de setup/teardown:

```csharp
public class AccountServiceTests : IAsyncLifetime
{
    private AccountService _accountService = null!;

    public Task InitializeAsync()   // ~ beforeEach
    {
        _accountService = new AccountService();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()      // ~ afterEach
    {
        return Task.CompletedTask;
    }
}
```

---

## 6. Comandos úteis no terminal

```bash
dotnet test                                    # roda todos os testes da solução
dotnet test --filter "FullyQualifiedName~Account"   # roda só os testes cujo nome contém "Account"
dotnet test --logger "console;verbosity=detailed"   # saída detalhada
dotnet test --collect:"XPlat Code Coverage"         # gera cobertura (coverlet)
```

### 6.1 Modo "loop" (equivalente ao watch do Vitest)

O Vitest fica observando os arquivos por padrão. No .NET, use o `dotnet watch`:

```bash
dotnet watch test --project ProjetoTestes/ProjetoTestes.Tests.csproj
dotnet watch test --project ProjetoTestes/ProjetoTestes.Tests.csproj --filter "FullyQualifiedName~ValidateCpf"
```

- A cada `.cs` salvo (teste ou `CpfValidator`), ele recompila e roda os testes de novo. Para sair, use `Ctrl+C`.
- O `--project` é obrigatório, porque o `dotnet watch` precisa saber qual projeto observar. Ele também observa os projetos referenciados (o `ProjetoTestes.Domain`).

#### Explicando cada parte do comando

```bash
dotnet watch test --project ProjetoTestes/ProjetoTestes.Tests.csproj --filter "FullyQualifiedName~ValidateCpf"
```

| Parte | O que faz |
|---|---|
| `dotnet` | A CLI do .NET, o programa que executa todos os comandos (`new`, `build`, `test`...) |
| `watch` | Fica **observando os arquivos**. A cada arquivo salvo, executa de novo o comando que vem depois dele. Sem o `watch`, o comando roda uma vez e termina |
| `test` | O comando que o `watch` repete: compila e roda os testes (o mesmo que `dotnet test`) |
| `--project ProjetoTestes/ProjetoTestes.Tests.csproj` | **Qual projeto** observar e testar. É o caminho do `.csproj` de testes. O `watch` também observa os projetos que ele referencia (`ProjetoTestes.Domain`), então mudar o `CpfValidator.cs` também dispara os testes |
| `--filter "..."` | Roda **só os testes que combinam** com a expressão, em vez de todos. As aspas são obrigatórias, porque a expressão tem caracteres especiais |
| `FullyQualifiedName` | A propriedade usada no filtro: o **nome completo** do teste, no formato `Namespace.Classe.Método`. Por exemplo, `ProjetoTestes.Tests.ValidateCpfTests.Deve_Verifidar_um_cpf_valido` |
| `~` | Operador **contém**. `FullyQualifiedName~ValidateCpf` significa "o nome completo contém `ValidateCpf`". Como a classe se chama `ValidateCpfTests`, todos os testes dela entram |
| `ValidateCpf` | O texto procurado |

> Atenção: a aspa precisa ser fechada no fim, como em `"FullyQualifiedName~ValidateCpf"`. Sem a aspa de fechamento, o terminal fica esperando o resto do comando.

**Operadores do `--filter`:**

| Operador | Significado | Exemplo |
|---|---|---|
| `~` | contém | `FullyQualifiedName~ValidateCpf` |
| `!~` | não contém | `FullyQualifiedName!~Integration` |
| `=` | igual (nome exato) | `FullyQualifiedName=ProjetoTestes.Tests.ValidateCpfTests.Deve_Verifidar_um_cpf_valido` |
| `!=` | diferente | `FullyQualifiedName!=...` |
| `\|` | OU | `"FullyQualifiedName~Cpf\|FullyQualifiedName~Email"` |
| `&` | E | `"FullyQualifiedName~Cpf&DisplayName~invalido"` |

**Propriedades que dá para usar no filtro (xUnit):**

| Propriedade | Compara com | Exemplo |
|---|---|---|
| `FullyQualifiedName` | `Namespace.Classe.Método` | `FullyQualifiedName~ValidateCpfTests` |
| `DisplayName` | O texto do `DisplayName` (ou o nome do método, se não houver) | `DisplayName~inválido` |
| `Category` (e outros *traits*) | `[Trait("Category", "Unit")]` no teste | `Category=Unit` |

**Teclas enquanto o `watch` está rodando:**

| Tecla | Ação |
|---|---|
| `Ctrl+R` | Força rodar de novo, sem precisar salvar um arquivo |
| `Ctrl+C` | Encerra o `watch` |

### 6.2 Relatório HTML com o resultado dos testes

O logger HTML já vem no `Microsoft.NET.Test.Sdk`. Não precisa instalar nada:

```powershell
dotnet test ProjetoTestes.slnx --logger "html;LogFileName=resultado-testes.html"
start ProjetoTestes\TestResults\resultado-testes.html
```

- **Sem `LogFileName`:** cada execução cria um arquivo novo, com data e hora no nome.
- **Vários formatos ao mesmo tempo:** repita o parâmetro, por exemplo `--logger html --logger trx`.
- **No VS Code:** o painel **Testing** (ícone de tubo de ensaio), do C# Dev Kit, é o mais próximo do `vitest --ui`.

### 6.3 Relatório de cobertura de código (linhas verdes e vermelhas)

É o relatório que mostra, linha a linha, o que os testes executaram: verde para executada, vermelho para não executada, e quantas vezes cada linha rodou. No Jest/Vitest isso vem do Istanbul. No .NET são duas ferramentas:

| Ferramenta | Papel | Como foi instalada |
|---|---|---|
| **coverlet.collector** | Coleta a cobertura e gera `coverage.cobertura.xml` | Já vem no template `xunit` |
| **ReportGenerator** | Transforma o XML em HTML | Ferramenta local: `dotnet tool install dotnet-reportgenerator-globaltool` |

Para gerar e abrir o relatório (PowerShell, na pasta da solução):

```powershell
Remove-Item TestResults -Recurse -Force -ErrorAction SilentlyContinue
dotnet test ProjetoTestes.slnx --collect:"XPlat Code Coverage" --results-directory TestResults
dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/coverage-report" -reporttypes:Html
start TestResults\coverage-report\index.html
```

1. **Remove-Item:** apaga os resultados antigos. Cada execução cria uma pasta nova com um GUID no nome, e sem limpar o relatório misturaria as execuções.
2. **dotnet test:** roda os testes, e o coverlet grava as linhas executadas.
3. **dotnet reportgenerator:** gera o HTML.
4. **start:** abre o relatório. Clique na classe (por exemplo, **CpfValidator**) para ver o código linha a linha.

**Direto no editor:** no painel **Testing** do C# Dev Kit, use **Run Tests with Coverage**, e as linhas ficam marcadas no próprio arquivo `.cs`.

**Resumo no terminal, sem abrir o HTML:** acrescente `TextSummary` em `-reporttypes` e leia o `Summary.txt`:

```powershell
dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/coverage-report" -reporttypes:"Html;TextSummary"
Get-Content TestResults\coverage-report\Summary.txt
```

### 6.4 Como ler o relatório de cobertura

#### Página inicial (`index.html`): o resumo

| Indicador | Significado |
|---|---|
| **Covered lines** | Linhas executáveis que rodaram pelo menos uma vez nos testes |
| **Uncovered lines** | Linhas executáveis que nenhum teste executou |
| **Coverable lines** | Total de linhas executáveis. Comentários, linhas em branco e declarações (`int d1, d2;`) não entram. No C#, algumas chaves `{ }` de blocos entram |
| **Total lines** | Todas as linhas do arquivo, incluindo comentários e linhas em branco |
| **Line coverage** | `Covered ÷ Coverable`. **É o percentual principal de cobertura** |
| **Branch coverage** | Percentual de **caminhos** de decisão testados. Cada `if`, `?:`, `&&` e `\|\|` tem dois lados (verdadeiro/falso), e cada lado é um branch |
| **Method coverage** | Percentual de métodos chamados pelo menos uma vez. Na versão gratuita do ReportGenerator aparece "Feature is only available for sponsors"; o valor existe no `Summary.txt` |

Mais abaixo vem a tabela **Coverage**, por **assembly** (projeto) e por **classe**, com as mesmas colunas. Clique no nome da classe para abrir o código.

#### Página da classe: as três partes

Ao clicar na classe (`ProjetoTestes.Domain_CpfValidator.html`), a página tem, de cima para baixo:

**1. Summary:** os mesmos números da página inicial, só para essa classe (linhas cobertas, branches, etc.).

**2. Metrics:** uma linha por método:

| Coluna | Significado |
|---|---|
| **Branch coverage** | % de caminhos do método testados |
| **Cyclomatic complexity** | Número de caminhos independentes no método: cada `if`, `&&`, `?:`, `for` e `catch` soma. Quanto maior, mais testes são necessários para cobrir tudo |
| **Crap Score** | Combina complexidade e falta de cobertura. Método complexo e pouco testado fica com nota alta, ou seja, é **arriscado** mexer nele. Abaixo de ~30 é aceitável; o ideal é chegar perto da própria complexidade (o que acontece com 100% de cobertura) |
| **Line coverage** | % de linhas do método cobertas |

**3. File(s):** o código-fonte linha a linha. Cada linha tem cinco colunas:

```
┌──┬──────┬─────┬────┬────────────────────────────────────────────────┐
│▌ │ hits │ nº  │ ⑂  │ código                                          │
├──┼──────┼─────┼────┼────────────────────────────────────────────────┤
│🟩│   4  │   7 │ ⑂  │ if (str != null)                                │
│🟩│  36  │  30 │    │ digito = int.Parse(str.Substring(nCount - 1, 1));│
│🟧│   4  │  39 │ ⑂  │ dg1 = (rest < 2) ? 0 : 11 - rest;               │
│🟩│   3  │  43 │    │ dg2 = 0;                                        │
│🟩│   1  │  45 │    │ dg2 = 11 - rest;                                │
│🟥│   0  │  55 │    │ Console.Error.WriteLine("Erro !" + e);          │
│  │      │  38 │    │ // se for menor que 2 é 0...                    │
└──┴──────┴─────┴────┴────────────────────────────────────────────────┘
```

| Coluna | O que mostra |
|---|---|
| **▌ barra colorida** | 🟩 verde: executada, com todos os branches testados. 🟧 laranja: executada, mas **só parte dos branches** testada. 🟥 vermelha: **nunca executada**. Sem cor: não executável (comentário, linha em branco, declaração) |
| **hits** | Quantas vezes a linha rodou, **somando todos os testes** (o `2x` do Istanbul). `0` é o mesmo que vermelho |
| **nº** | Número da linha no arquivo `.cs` |
| **⑂ ícone de bifurcação** | Aparece só em linhas com decisão (`if`, `?:`, `&&`). A barrinha embaixo do ícone mostra quanto dos caminhos foi testado |
| **código** | O fundo também fica verde, laranja ou vermelho claro |

**Passe o mouse sobre a linha.** A dica (*tooltip*) explica a marcação, por exemplo:
- `Covered (4 visits)`: rodou 4 vezes, tudo coberto.
- `Partially covered (4 visits, 2 of 4 branches are covered)`: rodou 4 vezes, mas só 2 dos 4 caminhos foram testados.
- `Not covered (0 visits)`: nunca rodou.
- `Not coverable`: linha não executável.

#### O HTML não se atualiza sozinho

O relatório é um arquivo estático, gerado no momento em que você roda o ReportGenerator. Depois de mudar um teste:

1. Rode o script `coverage.ps1`, na raiz da solução, para gerar tudo de novo.
2. Dê **F5** no navegador.

```powershell
.\coverage.ps1           # roda os testes, gera o HTML e abre o navegador
.\coverage.ps1 -NoOpen   # só gera; depois é só dar F5 na aba que já está aberta
```

O script também imprime o *Line coverage* e o *Branch coverage* no terminal, então dá para comparar sem abrir o HTML.

#### Experimento: pular um teste e ver a cobertura cair

A cobertura só conta o que **foi executado**. Teste pulado não executa, então as linhas que só ele cobria ficam vermelhas.

| Vitest | xUnit |
|---|---|
| `test.skip("...", ...)` | `[Fact(Skip = "motivo")]` ou `[Theory(Skip = "motivo")]`. O motivo é obrigatório |
| Pular só um caso do `test.each` | `[InlineData("71428793860", Skip = "motivo")]` |
| `test.only(...)` | Não existe no xUnit. Use `dotnet test --filter "DisplayName~parte do nome"` |

```csharp
[Theory(DisplayName = "Deve validar um cpf")]
[InlineData("97456321558")]
[InlineData("87748248800", Skip = "testando a cobertura sem este caso")]
public void Deve_validar_um_cpf(string cpf) { ... }
```

No resultado do `dotnet test`, o teste aparece como **Ignorado** (*Skipped*).

#### Exemplo real: o branch coverage muda sem o line coverage mudar

Ao trocar um caso de teste por `"714287938600"`, o resultado foi:

| | Antes | Depois |
|---|---|---|
| Line coverage | 81,5% (31/38) | 81,5% (31/38) |
| Branch coverage | 56,2% (9/16) | **62,5% (10/16)** |

Nenhuma linha nova foi executada, mas a linha 39 (`dg1 = (rest < 2) ? 0 : 11 - rest`) passou pela primeira vez pelo lado **verdadeiro** (`rest < 2`). Ganhou-se um caminho, não uma linha. É por isso que vale olhar os dois indicadores.

Esse caso também revelou um **bug do código legado**: `"714287938600"` tem **12 dígitos**, não é um CPF, e mesmo assim o validador retorna `true`. O código aceita entre 11 e 14 caracteres para permitir a formatação (`974.563.215-58`), mas, depois de limpar, nunca confere se sobraram exatamente 11 dígitos. Os testes servem justamente para encontrar e registrar esse tipo de comportamento antes de refatorar.

#### Por que não chegou a 100% (e como chegar)

Com 6 testes, o `CpfValidator` ficou em **86,8% de linhas** e **75% de branches**. Faltavam três coisas:

**1. Testar o tamanho de verdade (linhas 9 e 62).** O teste "mais dígitos que o necessário" usava `"11111111111111"`, com **14** caracteres. O validador aceita **de 11 a 14** (por causa da formatação `974.563.215-58`), então esse valor passava pela verificação de tamanho e só era recusado por ter dígitos iguais. A condição `str.Length >= 11 && str.Length <= 14` tem **4 branches**, porque cada lado do `&&` pode ser verdadeiro ou falso. Faltavam:
- um valor com **menos de 11** caracteres: `"123"`;
- um valor com **mais de 14** caracteres: `"123456789012345"`.

**2. Fazer o `catch` executar (linhas 53–57).** Ele só roda quando o `int.Parse` lança exceção. Falta um CPF com letra: `"a7456321558"`.

**3. Remover um branch impossível de testar (linha 55).** `Console.Error.WriteLine("Erro !" + e?.ToString())` tem 2 branches por causa do `?.` (se `e` é `null` ou não). Dentro de um `catch`, `e` **nunca** é `null`, então um dos lados nunca pode ser coberto. O mesmo acontece com `"Erro !" + e`, porque o compilador gera `e?.ToString()` por baixo. A correção no código de produção:

```csharp
Console.Error.WriteLine("Erro !" + e.Message);   // e.Message é string: sem branch extra
```

Resultado, verificado numa cópia do projeto com os 3 testes a mais e essa correção: **Line coverage 100%** e **Branch coverage 100% (14 de 14)**.

> **Lição:** às vezes o que impede os 100% não é falta de teste, mas um **caminho impossível** no código (código morto ou verificação desnecessária). Nesse caso, o certo é **simplificar o código**, e não inventar um teste. E 100% de cobertura não é garantia de que o código está certo: o `"714287938600"` (12 dígitos) é aceito como válido mesmo com tudo coberto.

#### Ver a cobertura direto no código (no editor)

Em vez de abrir o HTML, dá para ver as linhas cobertas e não cobertas dentro do próprio `.cs`, como na visualização do Istanbul.

**Opção 1: C# Dev Kit (já instalado)**

1. Abra o painel **Testing**: o ícone de tubo de ensaio na barra lateral, ou `Ctrl+Shift+P` → **Testing: Focus on Test Explorer View**.
2. No topo do painel, clique em **Run Tests with Coverage**: o ícone de play com um escudo. Pela paleta de comandos, é **Test: Run All Tests with Coverage**.
3. Abra o arquivo de produção (por exemplo, `CpfValidator.cs`). A margem esquerda fica marcada:
   - 🟩 **verde:** linha executada;
   - 🟥 **vermelho:** linha que nenhum teste executou;
   - **bifurcação amarela/laranja:** `if` com só um dos lados testado. Passe o mouse para ver quantos caminhos foram testados.
4. Também abre a visão **Test Coverage**, com o percentual por arquivo.
5. Para tirar as marcações, use **Test: Clear Coverage**, ou o botão de fechar na visão de cobertura.

**Opção 2: extensão Coverage Gutters** (`ryanluker.vscode-coverage-gutters`)

Ela lê o `coverage.cobertura.xml` gerado pelo `coverage.ps1`. Rode o script e clique em **Watch** na barra de status para as marcações aparecerem no editor.

#### Ver por onde **um teste específico** passou no código

Rodar a cobertura de **todos** os testes mistura os caminhos. Para ver o caminho de **um** teste:

**Opção 1: cobertura de um teste só**

1. No painel **Testing**, expanda até o teste (ou até um `[InlineData]` específico).
2. Clique com o botão direito no teste e escolha **Run Test with Coverage**.
3. Abra o `CpfValidator.cs`. Agora o verde mostra **só as linhas por onde aquele teste passou**, e o vermelho, as que ele não tocou.

**Opção 2: debug passo a passo (o mais completo)**

Mostra a execução linha a linha, com o valor de cada variável. Substitui o `console.log`.

1. Coloque um **breakpoint** (`F9`, ou clique à esquerda do número da linha) na linha do *When*, por exemplo `var result = CpfValidator.ValidateCpf(cpf);`.
2. No painel **Testing**, clique com o botão direito no teste e escolha **Debug Test**. Também dá para clicar no ícone ▷ ao lado do método no editor, com o botão direito.
3. A execução para no breakpoint. Use:

| Tecla | Ação |
|---|---|
| `F11` | **Step Into**: entra no método chamado (entra no `ValidateCpf`) |
| `F10` | **Step Over**: executa a linha atual e vai para a próxima |
| `Shift+F11` | **Step Out**: termina o método atual e volta para quem chamou |
| `F5` | **Continue**: segue até o próximo breakpoint ou até o fim |
| `Shift+F5` | **Stop**: para o debug |

   > **Atalho:** dá para colocar o breakpoint **direto no código de produção** (por exemplo, na primeira linha do `ValidateCpf` em `CpfValidator.cs`) e rodar **Debug Test** no teste. A execução para já dentro do validator, sem precisar do `F11`. Breakpoints em linhas por onde o teste não passa nunca são atingidos, o que também mostra o caminho.

4. Enquanto avança, observe:
   - a **linha amarela**, que mostra onde a execução está, ou seja, o caminho exato que o código seguiu (qual lado do `if` entrou);
   - o painel **Variables**, com os valores atuais (`str`, `d1`, `d2`, `dg1`, `rest`...);
   - o painel **Watch**, onde você pode adicionar expressões para acompanhar, como `d1 % 11` ou `nDigResult`;
   - os valores que aparecem ao passar o mouse sobre uma variável no código.

#### Ver onde um teste falhou

Também no painel **Testing**: o teste que falhou aparece com ❌. Clique nele para ver a mensagem (por exemplo, `Assert.False() Failure`), e o editor marca a linha do `Assert` que falhou.

Pelo terminal, o `dotnet test` mostra a mesma mensagem e o *stack trace*, com o arquivo e a linha do teste que falhou.

#### Lendo os números de `hits` (exemplo real do `CpfValidator` com 4 testes)

| Linha | hits | Por quê |
|---|---|---|
| 7: `if (str != null)` | 4 | Uma vez por teste (4 testes) |
| 17: `str.All(c => c == str[0])` | 12 | A lambda roda para cada caractere comparado: 3 por chamada × 4 |
| 30: dentro do `for` | 36 | 9 iterações × 4 testes |
| 28: o próprio `for` | 80 | Conta inicialização, comparação e incremento a cada volta |
| 43: `dg2 = 0` | 3 | 3 testes caíram no `if` |
| 45: `dg2 = 11 - rest` | 1 | 1 teste caiu no `else` |
| 53–57: `catch` | 0 | Nenhum teste provocou exceção, por isso está vermelho |

> Os hits das linhas 43 e 45 mostram **qual caminho cada teste seguiu**. Somados, dão 4, o número de testes.

> **Line coverage alto não basta.** Uma linha pode estar verde e ter um `if` testado só pelo lado verdadeiro. Por isso é preciso olhar também o **branch coverage** e as linhas amarelas.

#### Como usar a leitura para criar testes

1. Procure as linhas **vermelhas** e **amarelas**.
2. Para cada uma, pergunte: *que entrada faz o código passar por aqui?*
3. Escreva um teste com essa entrada e gere o relatório de novo.

Exemplo no `CpfValidator`, com 81,5% de linhas e 56,2% de branches, quando havia testes apenas com CPFs válidos:

| Linha | Situação | Entrada que cobre |
|---|---|---|
| 7: `if (str != null)` | 🟨 nunca foi `null` | `null` |
| 9: `if (str.Length >= 11 && ...)` | 🟨 tamanho sempre válido | `"123"`, `"1234567890123456"` |
| 17: `if (!str.All(...))` | 🟨 nunca com dígitos iguais | `"11111111111"` |
| 39: `dg1 = (rest < 2) ? 0 : ...` | 🟨 primeiro dígito nunca 0 | CPF com primeiro dígito verificador 0, por exemplo `"87748248800"` |
| 53–57: `catch` | 🟥 nunca houve exceção | CPF com letra, por exemplo `"a7456321558"` |
| 60, 62, 64: `else return false` | 🟥 nunca executado | Os mesmos casos das linhas 17, 9 e 7 |

> A pasta `TestResults/` é gerada a cada execução e deve ficar no `.gitignore`.

---

## 7. Bibliotecas que costumam acompanhar o xUnit

- **FluentAssertions** ou **Shouldly**: asserções em estilo fluente, mais parecidas com o `expect` do Jest. Exemplo com Shouldly: `account.Name.ShouldBe(input.Name)`. A partir da versão 8, a FluentAssertions passou a ter licença comercial; para estudo, a Shouldly é uma boa alternativa gratuita.
- **Moq** ou **NSubstitute**: criam *mocks* e *stubs*, equivalentes ao `jest.fn()` e ao `jest.mock()`.
- **Testcontainers**: sobe um banco real em Docker para testes de integração.
- **Bogus**: gera dados falsos (nomes, e-mails, documentos), parecido com o Faker.js.

---

## 8. Erros comuns

### xUnit1011: "There is no matching method parameter for value"

O teste tem `[InlineData("...")]`, mas o método não declara parâmetro para receber o valor:

```csharp
[Theory]
[InlineData("97456321558")]
public void Deve_validar_um_cpf()            // ❌ falta o parâmetro
public void Deve_validar_um_cpf(string cpf)  // ✅
```

Cada `[InlineData]` entrega seus valores aos parâmetros do método, na mesma ordem. É como os valores do `test.each` chegando como argumento da função.

### Relatório de cobertura vazio: "No assemblies have been covered" / N/A

**Sintoma:** os testes passam, mas o HTML mostra `N/A` em tudo, e o `coverage.cobertura.xml` fica minúsculo (~235 bytes).

**Causa:** havia um `dotnet watch test` rodando em outro terminal ao mesmo tempo. O coverlet funciona **alterando a DLL** do projeto testado (`ProjetoTestes.Domain.dll`) dentro da pasta `bin` para contar as execuções. Como o `watch` usa a mesma pasta `bin` e recompila ou mantém a DLL em uso, o coverlet não consegue instrumentá-la e não registra nada.

**Solução:** pare o `dotnet watch` (`Ctrl+C` no terminal dele) e rode o `coverage.ps1` de novo. O script agora avisa quando encontra um `dotnet watch test` rodando.

> O mesmo pode acontecer se uma sessão de **Debug Test** ou um **Run Tests with Coverage** do VS Code estiver rodando ao mesmo tempo. Rode uma coisa de cada vez.

---

## 9. Histórico do projeto (passo a passo do que foi feito)

Registro de tudo o que foi criado, instalado e configurado nesta solução, para conseguir reproduzir do zero.

### Estrutura atual

```
ProjetoTestes/                         ← raiz
├── ProjetoTestes.slnx                 ← solução
├── dotnet-tools.json                  ← ferramentas locais (ReportGenerator)
├── coverage.ps1                       ← gera o relatório de cobertura
├── ESTUDO-TESTES-XUNIT.md             ← este arquivo
├── ProjetoTestes.Domain/              ← código de produção (class library)
│   ├── ProjetoTestes.Domain.csproj
│   └── CpfValidator.cs
└── ProjetoTestes/                     ← projeto de testes (xUnit)
    ├── ProjetoTestes.Tests.csproj
    └── Unit/
        └── ValidateCpfTests.cs
```

### Passo 1: projeto de testes xUnit

```bash
dotnet new xunit -n ProjetoTestes.Tests -o ProjetoTestes.Tests
dotnet sln ProjetoTestes.slnx add ProjetoTestes.Tests/ProjetoTestes.Tests.csproj
```

O template `xunit` instala estes pacotes NuGet:

| Pacote | Versão | Para que serve |
|---|---|---|
| `xunit` | 2.9.3 | O framework de testes (`[Fact]`, `[Theory]`, `Assert`) |
| `xunit.runner.visualstudio` | 3.1.4 | Permite que `dotnet test`, o VS e o VS Code encontrem e rodem os testes |
| `Microsoft.NET.Test.Sdk` | 17.14.1 | Infraestrutura de testes do .NET (VSTest, loggers como o HTML) |
| `coverlet.collector` | 6.0.4 | Coleta de cobertura de código |

Depois, a pasta foi renomeada de `ProjetoTestes.Tests/` para `ProjetoTestes/`, e o caminho foi atualizado no `.slnx`.

### Passo 2: pasta `Unit`

Criada a pasta `ProjetoTestes/Unit/` para os testes de unidade. Não é preciso mexer no `.csproj`, porque projetos .NET nesse formato incluem automaticamente os `.cs` das subpastas.

### Passo 3: projeto de domínio (código de produção)

```bash
dotnet new classlib -n ProjetoTestes.Domain -o ProjetoTestes.Domain
dotnet sln ProjetoTestes.slnx add ProjetoTestes.Domain/ProjetoTestes.Domain.csproj
dotnet add ProjetoTestes/ProjetoTestes.Tests.csproj reference ProjetoTestes.Domain/ProjetoTestes.Domain.csproj
```

- O `Class1.cs` criado pelo template foi apagado.
- Foi criado o `CpfValidator.cs`, com o `validateCpf` da aula convertido de TypeScript para C#, mantendo a mesma lógica para depois refatorar com os testes cobrindo o comportamento.
- **Regra:** o projeto de testes referencia o de produção, e nunca o contrário.

Diferenças em relação à versão TypeScript:

1. **Remoção da formatação:** `.replace('.', '')` no JS remove só a primeira ocorrência. `Replace(".", "")` no C# remove todas.
2. **Nulo e `undefined`:** C# não tem `undefined`, então os dois testes viraram uma única verificação de `null`.
3. **Letra no CPF:** `int.Parse` lança exceção, que cai no `catch` e retorna `false`. No JS, o `parseInt` gerava `NaN` e a comparação falhava. O resultado é o mesmo.

### Passo 4: ReportGenerator (relatório de cobertura em HTML)

```bash
dotnet new tool-manifest                                   # cria o dotnet-tools.json
dotnet tool install dotnet-reportgenerator-globaltool      # versão 5.5.11
```

- Foi instalado como **ferramenta local**, registrada no `dotnet-tools.json`, e não global.
- Em outra máquina, depois de clonar, basta rodar `dotnet tool restore`.
- Como usar: veja a seção 6.3.

### Passo 5: script `coverage.ps1`

Criado na raiz da solução para não precisar digitar os quatro comandos de cobertura a cada vez. Ele apaga o `TestResults`, roda os testes com cobertura, gera o HTML e o `Summary.txt`, mostra os percentuais no terminal e abre o navegador (ou não, com `-NoOpen`). Veja a seção 6.4.
