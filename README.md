# PlayWithEliteItem

**Risk of Rain 2 · Random elite aspects · BepInEx · C#**

PlayWithEliteItem entrega um **equipamento de elite aleatório** para cada jogador elegível no início de uma partida. A distribuição acontece no **servidor/host**, usando o inventário original do jogo. Não adiciona HUD, comandos personalizados ou conteúdos novos.

[Thunderstore — versão pública](https://thunderstore.io/c/riskofrain2/p/CONDE/PlayWithEliteItem/) · [Baixar compilação de teste](https://github.com/HelioConde/PlayWithEliteItem/actions) · [Como testar em português](docs/TESTE_NO_JOGO.md) · [Resolver erros de carregamento](docs/TROUBLESHOOTING.md)

> **Atenção:** a versão pública **1.0.1** no Thunderstore ainda é a implementação antiga. O código **1.1.1** do GitHub é uma refatoração com comportamento diferente, **ainda não validada dentro do jogo**. Não foi publicada uma nova versão no Thunderstore.

## O que faz a versão 1.1.1

- Distribui um aspecto de elite no **slot de equipamento 0** (slot utilizado pela implementação atual).
- Usa o catálogo de equipamentos do RoR2; não cria itens ou aspectos novos.
- Dá preferência a aspectos diferentes, permitindo repetição quando todos já foram distribuídos.
- Respeita as expansões habilitadas na partida.
- Inclui jogadores que entram depois do começo da partida.
- Não substitui um equipamento já presente, salvo se a opção for ativada.

**Limitação:** o bônus depende do equipamento elite permanecer equipado. Não é um buff permanente independente; substituir o equipamento pode remover o efeito.

## Versões e dependências

| | Thunderstore | GitHub |
| --- | --- | --- |
| Versão | **1.0.1**, publicada | **1.1.1**, candidata a testes |
| Mecânica | Segundo slot e troca do mod antigo | Aspecto no slot ativo |
| Dependências | Pacote antigo com muitos módulos R2API | **1 dependência direta:** `bbepis-BepInExPack-5.4.2122` |
| Disponibilidade | Instalação pública | DLL de teste gerada pelo GitHub Actions |

O BepInExPack instala suas próprias dependências transitivas. **Uma dependência direta não significa um único pacote instalado.** A versão atual do código não usa R2API.

## Instalar a versão de teste

1. Crie um **perfil de teste separado** no r2modman/Thunderstore Mod Manager e instale o BepInExPack.
2. Abra [GitHub Actions](https://github.com/HelioConde/PlayWithEliteItem/actions) e escolha a última execução **Build and test mod** com status verde.
3. Baixe o artefato **PlayWithEliteItem**, extraia `GiveEliteItem.dll` e coloque-o em `BepInEx/plugins` do perfil de teste.
4. **Não mantenha a DLL antiga da versão 1.0.1 no mesmo perfil.** Inicie o jogo e comece uma partida.

**Se o arquivo de configuração não aparecer ou o log indicar falta de `Newtonsoft.Json 12.0.0.0`, veja o [diagnóstico de carregamento](docs/TROUBLESHOOTING.md).** Isso pode indicar uma instalação de BepInEx incompleta; não adicione DLLs de origem desconhecida.

Para testar a nova distribuição, use primeiro uma partida solo sem outros mods de gameplay. Confira o roteiro em [docs/TESTE_NO_JOGO.md](docs/TESTE_NO_JOGO.md).

## Configuração

O arquivo `BepInEx/config/AlchemyFlame.PlayWithEliteItem.cfg` é gerado ao iniciar o jogo com o plugin.

| Opção | Padrão | Efeito |
| --- | --- | --- |
| `General.Enabled` | `true` | Liga/desliga a distribuição |
| `Distribution.UniqueBeforeRepeats` | `true` | Evita repetições até esgotar os aspectos |
| `Distribution.IncludeAllRegisteredEliteAspects` | `false` | Inclui aspectos extras/modificados do catálogo (avançado) |
| `Distribution.AllowedEliteEquipment` | Sete elites clássicas | Lista de nomes internos separados por vírgula |
| `Equipment.ReplaceExistingEquipment` | `false` | Autoriza substituir um equipamento existente |
| `Diagnostics.VerboseLogging` | `false` | Registra detalhes no log do BepInEx |

Os sete nomes padrão: `EliteEarthEquipment`, `EliteFireEquipment`, `EliteHauntedEquipment`, `EliteIceEquipment`, `EliteLightningEquipment`, `EliteLunarEquipment`, `ElitePoisonEquipment`.

## Desenvolver

Requisito: **.NET SDK 8**.

```bash
dotnet restore GiveEliteItem.sln
dotnet build GiveEliteItem.sln -c Release --no-restore
dotnet run --project tests/PlayWithEliteItem.LogicTests/PlayWithEliteItem.LogicTests.csproj -c Release
python scripts/validate_manifest.py
```

**Organização do projeto:**

```text
GiveEliteItem/
  GiveEliteItem.cs         Entrada do plugin e ciclo de vida
  EliteSettings.cs         Configurações BepInEx
  EliteRunDistributor.cs   Jogadores, sorteio por partida e inventário
  EliteAspectCatalog.cs    Filtro de elites e DLCs habilitadas
  EliteEquipmentNames.cs   Leitura da lista configurada de elites
  AspectSelection.cs      Sorteio independente do jogo
tests/                     Testes da lógica de distribuição
scripts/                   Validação e montagem do pacote Thunderstore
thunderstore/              Manifesto e changelog da próxima publicação
docs/                      Testes manuais e orientações de publicação
.github/workflows/         Compilação, testes e ZIP candidato
```

O GitHub Actions verifica a compilação, executa testes da lógica e produz uma DLL e um ZIP **candidato**, sem publicar no Thunderstore. [Preparar publicação](docs/THUNDERSTORE_RELEASE.md).

## Antes de publicar

**Pendente:** comprovar efeitos, coleta/equipamento, mudanças de fase e multiplayer real com clientes sem o mod. Uma compilação aprovada **não comprova** que a mecânica funciona na partida.

Abra um [issue](https://github.com/HelioConde/PlayWithEliteItem/issues) com logs e passos de reprodução caso encontre problemas.

---

**Autoria:** AlchemyFlame · Licença [MIT](LICENSE) · Projeto não oficial de *Risk of Rain 2*.
