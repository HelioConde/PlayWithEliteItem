<div align="center">
  <h1>⚡ PlayWithEliteItem</h1>
  <p><strong>Risk of Rain 2 · Aspectos elite no segundo slot físico · Server-side</strong></p>
  <p>Um mod publicado para a comunidade, com uma implementação modernizada em testes.</p>
  <p>
    <a href="https://thunderstore.io/c/riskofrain2/p/CONDE/PlayWithEliteItem/"><img alt="Thunderstore 1.0.1 publicada" src="https://img.shields.io/badge/Thunderstore-1.0.1%20publicada-16803c?style=for-the-badge"></a>
    <a href="https://github.com/HelioConde/PlayWithEliteItem/actions"><img alt="GitHub 1.2.1 candidata a testes" src="https://img.shields.io/badge/GitHub-1.2.1%20em%20testes-64748b?style=for-the-badge"></a>
  </p>
  <p><a href="https://thunderstore.io/c/riskofrain2/p/CONDE/PlayWithEliteItem/"><strong>Versão pública</strong></a> · <a href="https://github.com/HelioConde/PlayWithEliteItem/actions"><strong>Build experimental</strong></a> · <a href="docs/TESTE_NO_JOGO.md"><strong>Guia de validação</strong></a></p>
</div>

---

## Visão geral

O mod dá um **aspecto de elite aleatório** a cada jogador no começo da partida, guardado no **slot físico 1**. O slot normal **Q (slot 0)** continua livre para usar Radar, Tricórnio e outros equipamentos.

Na versão **1.2.1**, o elite **não troca de slot**. O teste da 1.2.0 revelou que estar visível no segundo slot não garante seus poderes. A correção agora mantém o **BuffDef passivo original** no servidor quando o elite está no slot 1 e o Q contém outro equipamento. Não há HUD nova, teclas extras ou mensagens de rede personalizadas.

> **Thunderstore: 1.0.1 (antiga). GitHub: 1.2.1 (candidata a testes).** A nova versão ainda precisa de validação completa em jogo antes de ser publicada no Thunderstore.

## Funcionamento

1. Ao aparecer na partida, o servidor sorteia um aspecto elite e o entrega com `SetEquipmentIndexForSlot(elite, 1)` — técnica usada no projeto original de 2024.
2. O elite fica **fixo no slot físico 1**, ao lado do Q, sem ser trocado por uso de equipamentos.
3. O jogador coleta e utiliza qualquer equipamento normal no **Q**, sujeito às regras normais do Risk of Rain 2.
4. Ao trocar o equipamento do Q, o mod **não interfere na coleta** e não modifica o slot elite. Uma rotina no servidor mantém o **buff original daquele elite** enquanto o slot secundário estiver ocupado, sem criar novo tipo de buff.

**Não há troca automática**, nem mesmo ao apertar Q. A aplicação do buff pelo servidor é experimental: certos elites podem depender de comportamentos adicionais além do BuffDef. Teste efeitos de combate e não apenas a aparência.

Para segurança, se outro sistema já tiver colocado um equipamento no slot 1, o mod **não o substitui**. Se a API não conseguir atribuir o elite ao slot físico 1, o log informa a falha; não existe mais fallback que altere o Q.

## Instalação para teste

1. Abra um perfil separado no r2modman com **BepInExPack** completo e suas dependências; inicie com **Start modded**.
2. Abra o [GitHub Actions](https://github.com/HelioConde/PlayWithEliteItem/actions) e escolha o último build aprovado.
3. Baixe o artefato **PlayWithEliteItem**, extraia `GiveEliteItem.dll` e instale em `BepInEx/plugins` desse perfil.
4. Apague **todas as DLLs antigas** do PlayWithEliteItem desse perfil. O mod deve carregar apenas uma vez.
5. Comece uma partida e confirme o elite no **ícone à direita do Q**. Pegue um equipamento, use-o e depois substitua-o; o elite deve continuar no segundo slot.

Esta versão não usa hooks de equipamentos e, portanto, **não depende diretamente de HookGenPatcher**. Perfis com outros mods podem mantê-lo instalado se precisarem dele.

## Configuração

O arquivo `BepInEx/config/AlchemyFlame.PlayWithEliteItem.cfg` é criado pelo BepInEx.

| Configuração | Padrão | Função |
| --- | --- | --- |
| `General.Enabled` | true | Ativa distribuição de elite |
| `Distribution.UniqueBeforeRepeats` | true | Evita repetições até esgotar elites |
| `Distribution.IncludeAllRegisteredEliteAspects` | false | Descoberta automática de elites extras |
| `Distribution.AllowedEliteEquipment` | Sete elites clássicas | Nomes internos separados por vírgula |
| `Elite.MaintainPassiveWhileStored` | true | Mantém o buff passivo do elite guardado no slot 1, sem trocá-lo |
| `Diagnostics.TraceGameplay` | true | Registra concessão, manutenção do buff e verificação dos dois slots |

Configurações antigas `Switching.*`, `Equipment.PreferOriginalPhysicalSlot` e `Elite.KeepEliteBuffWhileReserved` **não têm efeito na versão 1.2.1**. Podem permanecer no CFG, mas não são mais lidas.

## Dependências

**Uma dependência direta:** `bbepis-BepInExPack-5.4.2122`. O BepInExPack possui suas próprias dependências transitivas. Esta versão não exige o antigo conjunto R2API nem HookGenPatcher diretamente.

## Desenvolvimento

```bash
dotnet restore GiveEliteItem.sln
dotnet build GiveEliteItem.sln -c Release --no-restore
dotnet run --project tests/PlayWithEliteItem.LogicTests/PlayWithEliteItem.LogicTests.csproj -c Release
python scripts/validate_manifest.py
```

- `GiveEliteItem.cs`: inicialização BepInEx.
- `EliteSettings.cs`: opções ativas.
- `EliteRunDistributor.cs`: sorteio, concessão no slot físico 1 e manutenção do BuffDef elite no servidor.
- `EliteAspectCatalog.cs` / `AspectSelection.cs`: descoberta e sorteio.
- `EliteSlotPolicy.cs`: regras de proteção do slot e testes puros.

O CI testa a lógica e compila o mod; **não executa Risk of Rain 2**. O ZIP de publicação só é um candidato, não um lançamento.

Licença [MIT](LICENSE) · Criado por AlchemyFlame · Mod não oficial.
