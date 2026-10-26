# PlayWithEliteItem

**Risk of Rain 2 · Equipamento de elite no segundo slot · Server-side · C#**

Mod de AlchemyFlame que concede um equipamento de elite ao nascer e permite guardar outro equipamento no servidor. **A versão 1.1.8 corrige a dependência ausente `MMHOOK_RoR2.dll`**: o código anterior não chegou a iniciar no seu jogo. A novidade é manter o buff de elite enquanto o equipamento fica na reserva do servidor.

[Versão pública no Thunderstore](https://thunderstore.io/c/riskofrain2/p/CONDE/PlayWithEliteItem/) · [Baixar DLL de teste](https://github.com/HelioConde/PlayWithEliteItem/actions) · [Roteiro de testes](docs/TESTE_NO_JOGO.md)

> **Publicada:** 1.0.1 · **Em testes no GitHub:** 1.1.8. O código novo **não foi testado em uma partida real**. Não faça upload no Thunderstore antes de validar com o jogo.

## Como funciona

1. Quando o personagem aparece no mapa, o servidor escolhe e equipa um aspecto elite **no slot ativo Q**. O ícone elite deve aparecer imediatamente, com seu efeito passivo.
2. Ao pegar um equipamento, como o **Radar Escaneador**, o novo equipamento ocupa o Q e o elite vai para o **segundo slot lógico** (reserva). **O elite não deve cair no chão.**
3. Enquanto o elite está na reserva, o mod mantém seu **efeito passivo** no personagem usando o BuffDef original do jogo.
4. Depois de **usar com sucesso** o equipamento ativo, o servidor aguarda **0,3 segundo** e troca o equipamento ativo pelo armazenado, preservando o estado de cargas e recarga.
5. Se o elite estiver ativo quando o jogador coletar outro equipamento, o mod tenta guardar o elite novamente e deixa a coleta substituir o equipamento normal.

**Importante:** a reserva é **virtual, no servidor**, não um segundo quadrado visível na HUD. O elite deve aparecer no Q ao nascer. Depois de coletar outro equipamento, ficará guardado (seu efeito passivo será **mantido pelo servidor** enquanto estiver na reserva).

Não interceptamos inventários de múltiplos slots/conjuntos, como MUL-T e Functional Coupler, até validar sua compatibilidade. Aspectos elite em geral **não podem ser ativados**. Como a troca ainda depende de um uso bem-sucedido, um elite que volte ao slot ativo pode impedir que o Radar seja recuperado por ativação. Esta é uma **limitação conhecida ainda pendente de solução**.

## Instalação da versão de teste

1. Instale [BepInExPack](https://thunderstore.io/c/riskofrain2/p/bbepis/BepInExPack/) **e [HookGenPatcher 1.2.9](https://thunderstore.io/c/riskofrain2/p/RiskofThunder/HookGenPatcher/)** no mesmo perfil do r2modman, aceitando suas dependências. Inicie com **Start modded**: HookGenPatcher gera `MMHOOK_RoR2.dll` antes do carregamento do plugin.
2. No [GitHub Actions](https://github.com/HelioConde/PlayWithEliteItem/actions), abra a última execução **Build and test mod** aprovada.
3. Baixe o artefato **PlayWithEliteItem**, extraia `GiveEliteItem.dll` e coloque-o em `BepInEx/plugins` do perfil ativo.
4. Remova qualquer cópia da DLL 1.0.1/1.1.1 desse mesmo perfil e não use `Risk of Rain 2/Mods` para esse plugin.
5. Inicie uma partida e teste pegar o Radar, usá-lo e observar a troca. Siga o [roteiro](docs/TESTE_NO_JOGO.md).

Se o plugin não iniciar com `FileNotFoundException: MMHOOK_RoR2`, instale o HookGenPatcher no **mesmo perfil**. Veja o [diagnóstico do erro](docs/TROUBLESHOOTING.md). Se ele carregar mas o Q continuar vazio, verifique no `BepInEx/LogOutput.log` as mensagens prefixadas por `[PWEI]`: `[BOOT]`, `[RUN]`, `[CATALOG]`, `[SCAN]`, `[GRANT]`, `[VERIFY]`, `[PICKUP]` e `[SWAP]`.

## Configuração

O BepInEx cria `BepInEx/config/AlchemyFlame.PlayWithEliteItem.cfg` após carregar o mod.

| Configuração | Padrão | Significado |
| --- | --- | --- |
| `General.Enabled` | true | Ativa o mod |
| `Distribution.UniqueBeforeRepeats` | true | Evita repetir aspectos até esgotar opções |
| `Distribution.IncludeAllRegisteredEliteAspects` | false | Inclui aspectos extras registrados |
| `Distribution.AllowedEliteEquipment` | Sete elites clássicas | Nomes internos separados por vírgula |
| `Switching.AutomaticSwapAfterUse` | true | Habilita a troca automática |
| `Elite.KeepEliteBuffWhileReserved` | **true** | Mantém o BuffDef passivo quando o elite fica guardado |
| `Switching.SwapDelaySeconds` | 0.3 | Atraso da troca, de 0 a 2 segundos |
| `Diagnostics.VerboseLogging` | false | Mensagens extras opcionais |
| `Diagnostics.TraceGameplay` | **true** | Diagnóstico de inicialização, catálogo, jogador, concessão e troca; limitado para não inundar o log |

O antigo parâmetro `Equipment.ReplaceExistingEquipment` não é mais utilizado. Configurações antigas podem continuar no CFG. Caso outro mod forneça equipamento no início, ele será preservado na reserva antes de equipar o elite.

## Dependências

**Duas dependências diretas**: `bbepis-BepInExPack-5.4.2122` e `RiskofThunder-HookGenPatcher-1.2.9`. HookGenPatcher gera `MMHOOK_RoR2.dll`, exigida pelos hooks `On.RoR2` deste plugin. A biblioteca `MMHOOK.RoR2` do projeto C# serve apenas à compilação e não instala automaticamente a DLL no perfil do jogo. A suíte R2API completa **não é necessária**. Ambos os pacotes podem ter dependências transitivas.

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
