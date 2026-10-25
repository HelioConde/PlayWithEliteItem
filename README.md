# PlayWithEliteItem

**Risk of Rain 2 · Equipamento de elite no segundo slot · Server-side · C#**

Mod de AlchemyFlame que dá a cada jogador um **aspecto de elite aleatório** e permite utilizar outro equipamento sem perder o elite. A técnica original do projeto troca equipamentos após o uso; a versão de teste **1.1.2 restaura essa mecânica** usando uma reserva lógica mantida no servidor, compatível com a ideia de não instalar interfaces nos clientes.

[Versão pública no Thunderstore](https://thunderstore.io/c/riskofrain2/p/CONDE/PlayWithEliteItem/) · [Baixar DLL de teste](https://github.com/HelioConde/PlayWithEliteItem/actions) · [Roteiro de testes](docs/TESTE_NO_JOGO.md)

> **Publicada:** 1.0.1 · **Em testes no GitHub:** 1.1.2. O código novo **não foi testado em uma partida real**. Não faça upload no Thunderstore antes de validar com o jogo.

## Como funciona

1. Quando a partida começa, o servidor escolhe um aspecto elite para cada jogador e o guarda no **segundo slot lógico** (reserva). O slot ativo do jogo continua livre.
2. Ao pegar um equipamento, como o **Radar Escaneador**, ele ocupa o slot ativo normalmente. O elite continua guardado em vez de cair no chão.
3. Depois de **usar com sucesso** o equipamento ativo, o servidor aguarda **0,3 segundo** e troca o equipamento ativo pelo armazenado, preservando o estado de cargas e recarga.
4. Se o elite estiver ativo quando o jogador coletar outro equipamento, o mod tenta guardar o elite novamente e deixa a coleta substituir o equipamento normal.

**Importante:** a reserva é **virtual, no servidor**, e não um segundo quadrado visível na HUD. Isso evita exigir alterações na interface de jogadores sem o mod. O aspecto elite só fornece o efeito passivo enquanto estiver realmente equipado no slot ativo.

Não interceptamos inventários de múltiplos slots/conjuntos, como MUL-T e Functional Coupler, até validar sua compatibilidade. Equipamentos sem ação ativável podem impedir uma troca automática de volta, pois a mecânica é acionada pelo **uso bem-sucedido** do equipamento — comportamento que precisamos avaliar em jogo.

## Instalação da versão de teste

1. Instale [BepInExPack](https://thunderstore.io/c/riskofrain2/p/bbepis/BepInExPack/) **com as dependências** pelo r2modman. Execute o jogo com **Start modded**.
2. No [GitHub Actions](https://github.com/HelioConde/PlayWithEliteItem/actions), abra a última execução **Build and test mod** aprovada.
3. Baixe o artefato **PlayWithEliteItem**, extraia `GiveEliteItem.dll` e coloque-o em `BepInEx/plugins` do perfil ativo.
4. Remova qualquer cópia da DLL 1.0.1/1.1.1 desse mesmo perfil e não use `Risk of Rain 2/Mods` para esse plugin.
5. Inicie uma partida e teste pegar o Radar, usá-lo e observar a troca. Siga o [roteiro](docs/TESTE_NO_JOGO.md).

Se o plugin nem carregar, veja o [diagnóstico de bibliotecas BepInEx](docs/TROUBLESHOOTING.md).

## Configuração

O BepInEx cria `BepInEx/config/AlchemyFlame.PlayWithEliteItem.cfg` após carregar o mod.

| Configuração | Padrão | Significado |
| --- | --- | --- |
| `General.Enabled` | true | Ativa o mod |
| `Distribution.UniqueBeforeRepeats` | true | Evita repetir aspectos até esgotar opções |
| `Distribution.IncludeAllRegisteredEliteAspects` | false | Inclui aspectos extras registrados |
| `Distribution.AllowedEliteEquipment` | Sete elites clássicas | Nomes internos separados por vírgula |
| `Switching.AutomaticSwapAfterUse` | true | Habilita a troca automática |
| `Switching.SwapDelaySeconds` | 0.3 | Atraso da troca, de 0 a 2 segundos |
| `Diagnostics.VerboseLogging` | false | Registra detalhes do sistema |

O antigo parâmetro `Equipment.ReplaceExistingEquipment` deixou de ser necessário: o elite não ocupa o slot ativo no início. Configurações antigas podem continuar no arquivo CFG, mas não serão utilizadas.

## Dependências

**Direta:** apenas `bbepis-BepInExPack-5.4.2122`. Seus requisitos transitivos são instalados pelo gerenciador. O projeto não utiliza R2API nem adiciona conteúdo ou RPCs. O projeto de compilação usa `MMHOOK.RoR2` para declarar os hooks no C#, sem exigir R2API como dependência direta.

## Código e testes

```bash
dotnet restore GiveEliteItem.sln
dotnet build GiveEliteItem.sln -c Release --no-restore
dotnet run --project tests/PlayWithEliteItem.LogicTests/PlayWithEliteItem.LogicTests.csproj -c Release
```

O plugin é organizado em entrada (`GiveEliteItem.cs`), configurações (`EliteSettings.cs`), catálogo (`EliteAspectCatalog.cs`), seleção (`AspectSelection.cs`) e **distribuição/troca** (`EliteRunDistributor.cs`). `EliteRotationRules.cs` testa as decisões de troca e proteção de coleta.

**Ainda falta testar de verdade:** carregamento, coleta sem drop do elite, troca após uso, buffs, jogador que entra depois, recargas, multiplayer server-side e situações com vários equipamentos. O CI compila e executa testes de lógica, mas não executa o jogo.

## Publicação

[Preparação do pacote Thunderstore](docs/THUNDERSTORE_RELEASE.md). **Nenhuma versão nova foi enviada ao Thunderstore.**

Licença [MIT](LICENSE) · AlchemyFlame · Mod não oficial de Risk of Rain 2.
